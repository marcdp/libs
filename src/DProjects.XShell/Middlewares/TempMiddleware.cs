using DProjects.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DProjects.XShell.Middlewares {

    public sealed class TempMiddleware : IDisposable {

        // fields
        private readonly RequestDelegate mNext;
        private readonly Extensions.TempConfig mConfig;
        private readonly FileExtensionContentTypeProvider mContentTypes = new();
        private readonly Timer mTimer;

        // ctor
        public TempMiddleware(RequestDelegate next, Extensions.TempConfig tempConfig) {
            mNext = next;
            mConfig = tempConfig;
            Directory.CreateDirectory(mConfig.Path);
            // create a cron that deletes expired files every X minutes 
            mTimer = new System.Threading.Timer(_ => {
                try {
                    var now = DateTime.UtcNow;
                    foreach (var dir in Directory.GetDirectories(mConfig.Path)) {
                        var id = Path.GetFileName(dir);
                        if (!Guid.TryParse(id, out System.Guid _)) continue;
                        var info = new DirectoryInfo(dir);
                        var expiration = info.CreationTimeUtc.Add(mConfig.ExpirationTime);
                        if (expiration < now) {
                            try {
                                Directory.Delete(dir, recursive: true);
                            } catch { }
                        }
                    }
                } catch { }
            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
        public void Dispose() {
            mTimer.Dispose(); 
        }


        // methods
        public async Task InvokeAsync(HttpContext context) {
            // check if the request path starts with the middleware's configured base path
            if (!context.Request.Path.StartsWithSegments(mConfig.BasePath, out var remaining)) {
                await mNext(context);
                return;
            }

            // route POST requests to the upload handler when path exactly matches the base
            if (HttpMethods.IsPost(context.Request.Method) && remaining == PathString.Empty) {
                await PostAsync(context);
                return;
            }

            // route GET requests to the download handler
            if (HttpMethods.IsGet(context.Request.Method)) {
                await GetAsync(context, remaining);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        }


        // methods (private)
        private async Task PostAsync(HttpContext context) {
            // reject requests that are not form submissions (multipart/form-data)
            if (!context.Request.HasFormContentType) {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            // read the form data from the request
            var form = await context.Request.ReadFormAsync(context.RequestAborted);

            // require exactly one uploaded file
            if (form.Files.Count != 1) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // take the uploaded file and sanitize its filename
            var file = form.Files[0];

            // check file size limit
            if (file.Length > mConfig.FileSizeLimit) {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return;
            }

            var filename = GetFilename(file.FileName);

            // reject invalid filenames
            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            // create a unique directory for this upload and compute the target file path
            var id = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(mConfig.Path, id);
            var filePath = Path.Combine(directory, filename);

            Directory.CreateDirectory(directory);

            // save the uploaded file to disk asynchronously
            await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) {
                await file.CopyToAsync(output, context.RequestAborted);
            }

            // respond with 201 Created and return the URL in the body
            context.Response.StatusCode = StatusCodes.Status201Created;

            // build the public URL for the saved file
            var url = $"temp:/{id}/{Uri.EscapeDataString(filename)}?size={file.Length}&type={MimeTypeUtils.GetMimeType(filename)}&hash={ComputeHash(filePath)}&expiration={System.DateTimeOffset.UtcNow.Add(mConfig.ExpirationTime).ToUnixTimeSeconds()}";
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(url, context.RequestAborted);
        }

        private string ComputeHash(string filePath) {
            // compute SHA256 hash of the file for integrity verification
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // handles GET requests: validates id/filename and streams the file back
        private async Task GetAsync(HttpContext context, PathString remaining) {
            // split the remaining path into expected [id, filename] segments
            var parts = (remaining.Value ?? "").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // validate expected format: {id}/{filename} where id is a GUID
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out System.Guid _)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // sanitize the filename extracted from the URL
            var filename = GetFilename(Uri.UnescapeDataString(parts[1]));

            // if filename invalid, return 404
            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            // compose the physical paths for the stored file
            var directory = Path.Combine(mConfig.Path, parts[0]);
            var filePath = Path.Combine(directory, filename);

            // ensure the file exists and the path does not escape the intended directory
            if (!IsInside(filePath, directory) || !File.Exists(filePath)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            // determine the response content type for the filename
            if (!mContentTypes.TryGetContentType(filename, out var contentType)) {
                contentType = "application/octet-stream";
            }
            // get file info to set Content-Length
            var info = new FileInfo(filePath);

            context.Response.ContentType = contentType;
            context.Response.ContentLength = info.Length;

            // open the file and stream it to the response body
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);

            // copy
            await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        private static string? GetFilename(string filename) {
            // sanitize the filename to prevent directory traversal and invalid characters
            if (String.IsNullOrWhiteSpace(filename)) return null;
            filename = filename.Replace('\\', '/');
            filename = filename[(filename.LastIndexOf('/') + 1)..];
            if (filename is "" or "." or "..") return null;
            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
            return filename;
        }
        private static bool IsInside(string path, string root) {
            // use case-insensitive comparison on Windows
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            // normalize both paths and ensure root ends with a directory separator
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path);
            // confirm path starts with the normalized root
            return path.StartsWith(root, comparison);
        }


    }

}