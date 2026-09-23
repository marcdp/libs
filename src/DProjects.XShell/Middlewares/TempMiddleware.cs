using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DProjects.XShell.Middlewares {

    public sealed class TempMiddleware {


        // fields
        private readonly RequestDelegate mNext;
        private readonly string mPhysicalPath;
        private readonly string mRequestPath;
        private readonly FileExtensionContentTypeProvider mContentTypes = new();


        // ctor
        public TempMiddleware(RequestDelegate next, string physicalPath, string requestPath) {
            mNext = next;
            mPhysicalPath = Path.GetFullPath(physicalPath);
            mRequestPath = NormalizeRequestPath(requestPath);

            Directory.CreateDirectory(mPhysicalPath);
        }


        // methods
        public async Task InvokeAsync(HttpContext context) {
            if (!context.Request.Path.StartsWithSegments(mRequestPath, out var remaining)) {
                await mNext(context);
                return;
            }

            if (HttpMethods.IsPost(context.Request.Method) && remaining == PathString.Empty) {
                await PostAsync(context);
                return;
            }

            if (HttpMethods.IsGet(context.Request.Method)) {
                await GetAsync(context, remaining);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        }


        // methods (private)
        private async Task PostAsync(HttpContext context) {
            if (!context.Request.HasFormContentType) {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            var form = await context.Request.ReadFormAsync(context.RequestAborted);

            if (form.Files.Count != 1) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var file = form.Files[0];
            var filename = GetFilename(file.FileName);

            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var id = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(mPhysicalPath, id);
            var filePath = Path.Combine(directory, filename);

            Directory.CreateDirectory(directory);

            await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) {
                await file.CopyToAsync(output, context.RequestAborted);
            }

            var url = $"{mRequestPath}/{id}/{Uri.EscapeDataString(filename)}";

            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.Headers.Location = url;

            await context.Response.WriteAsJsonAsync(new { url }, context.RequestAborted);
        }

        private async Task GetAsync(HttpContext context, PathString remaining) {
            var parts = (remaining.Value ?? "")
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 || !Guid.TryParse(parts[0], out _)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var filename = GetFilename(Uri.UnescapeDataString(parts[1]));

            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var directory = Path.Combine(mPhysicalPath, parts[0]);
            var filePath = Path.Combine(directory, filename);

            if (!IsInside(filePath, directory) || !File.Exists(filePath)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!mContentTypes.TryGetContentType(filename, out var contentType)) {
                contentType = "application/octet-stream";
            }

            var info = new FileInfo(filePath);

            context.Response.ContentType = contentType;
            context.Response.ContentLength = info.Length;

            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
        }

        private static string NormalizeRequestPath(string path) {
            if (String.IsNullOrWhiteSpace(path)) {
                throw new ArgumentException("Request path cannot be empty.", nameof(path));
            }

            path = "/" + path.Trim('/');

            if (path == "/") {
                throw new ArgumentException("Request path cannot be root.", nameof(path));
            }

            return path;
        }

        private static string? GetFilename(string filename) {
            if (String.IsNullOrWhiteSpace(filename)) return null;

            filename = filename.Replace('\\', '/');
            filename = filename[(filename.LastIndexOf('/') + 1)..];

            if (filename is "" or "." or "..") return null;

            return filename;
        }

        private static bool IsInside(string path, string root) {
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path);

            return path.StartsWith(root, comparison);
        }


    }

}