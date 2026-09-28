using DProjects.Utils;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace DProjects.XShell.Middlewares {

    public sealed class TempMiddleware {

        // Holds middleware dependencies and configured physical/request paths
        // fields
        private readonly RequestDelegate mNext;
        private readonly string mPhysicalPath;
        private readonly string mRequestPath;
        private readonly FileExtensionContentTypeProvider mContentTypes = new();


        // Constructor: initializes paths and ensures the temp directory exists
        // ctor
        public TempMiddleware(RequestDelegate next, string physicalPath, string requestPath) {
            mNext = next;
            mPhysicalPath = Path.GetFullPath(physicalPath);
            mRequestPath = NormalizeRequestPath(requestPath);

            Directory.CreateDirectory(mPhysicalPath);
        }


        // Main request dispatcher: routes incoming requests to handlers based on path and method
        // methods
        public async Task InvokeAsync(HttpContext context) {
            // Check if the request path starts with the middleware's configured base path
            if (!context.Request.Path.StartsWithSegments(mRequestPath, out var remaining)) {
                await mNext(context);
                return;
            }

            // Route POST requests to the upload handler when path exactly matches the base
            if (HttpMethods.IsPost(context.Request.Method) && remaining == PathString.Empty) {
                await PostAsync(context);
                return;
            }

            // Route GET requests to the download handler
            if (HttpMethods.IsGet(context.Request.Method)) {
                await GetAsync(context, remaining);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        }


        // Handles POST uploads: validates form, saves file, and returns URL
        // methods (private)
        private async Task PostAsync(HttpContext context) {
            // Reject requests that are not form submissions (multipart/form-data)
            if (!context.Request.HasFormContentType) {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            // Read the form data from the request
            var form = await context.Request.ReadFormAsync(context.RequestAborted);

            // Require exactly one uploaded file
            if (form.Files.Count != 1) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Take the uploaded file and sanitize its filename
            var file = form.Files[0];
            var filename = GetFilename(file.FileName);

            // Reject invalid filenames
            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            // Create a unique directory for this upload and compute the target file path
            var id = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(mPhysicalPath, id);
            var filePath = Path.Combine(directory, filename);

            Directory.CreateDirectory(directory);

            // Save the uploaded file to disk asynchronously
            await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) {
                await file.CopyToAsync(output, context.RequestAborted);
            }

            // Respond with 201 Created and return the URL in the body
            context.Response.StatusCode = StatusCodes.Status201Created;

            // Build the public URL for the saved file
            var url = $"temp:/{id}/{Uri.EscapeDataString(filename)}?size={file.Length}&type={MimeTypeUtils.GetMimeType(filename)}";
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(url, context.RequestAborted);
        }

        // Handles GET requests: validates id/filename and streams the file back
        private async Task GetAsync(HttpContext context, PathString remaining) {
            // Split the remaining path into expected [id, filename] segments
            var parts = (remaining.Value ?? "").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Validate expected format: {id}/{filename} where id is a GUID
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out _)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // Sanitize the filename extracted from the URL
            var filename = GetFilename(Uri.UnescapeDataString(parts[1]));

            // If filename invalid, return 404
            if (filename == null) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            // Compose the physical paths for the stored file
            var directory = Path.Combine(mPhysicalPath, parts[0]);
            var filePath = Path.Combine(directory, filename);

            // Ensure the file exists and the path does not escape the intended directory
            if (!IsInside(filePath, directory) || !File.Exists(filePath)) {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            // Determine the response content type for the filename
            if (!mContentTypes.TryGetContentType(filename, out var contentType)) {
                contentType = "application/octet-stream";
            }
            // Get file info to set Content-Length
            var info = new FileInfo(filePath);

            context.Response.ContentType = contentType;
            context.Response.ContentLength = info.Length;

            // Open the file and stream it to the response body
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
        }

        // Normalize and validate configured request path (must not be empty or root)
        // Validate and normalize the configured request path
        private static string NormalizeRequestPath(string path) {
            // Ensure the provided path is not null/empty/whitespace
            if (String.IsNullOrWhiteSpace(path)) {
                throw new ArgumentException("Request path cannot be empty.", nameof(path));
            }

            // Trim extra slashes and ensure it starts with a single '/'
            path = "/" + path.Trim('/');

            // Root path is not allowed for this middleware
            if (path == "/") {
                throw new ArgumentException("Request path cannot be root.", nameof(path));
            }

            return path;
        }

        // Extract a safe filename from input, reject path traversal and empty names
        // Extract a safe filename by stripping path segments and rejecting invalid names
        private static string? GetFilename(string filename) {
            // Reject empty or whitespace names
            if (String.IsNullOrWhiteSpace(filename)) return null;

            // Normalize separators and take last path segment (basename)
            filename = filename.Replace('\\', '/');
            filename = filename[(filename.LastIndexOf('/') + 1)..];

            // Reject filenames that are empty or navigation tokens
            if (filename is "" or "." or "..") return null;

            return filename;
        }

        // Verify that 'path' is located inside 'root' to prevent directory escape
        // Ensure the resolved path is contained within the given root directory
        private static bool IsInside(string path, string root) {
            // Use case-insensitive comparison on Windows
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            // Normalize both paths and ensure root ends with a directory separator
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path);

            // Confirm path starts with the normalized root
            return path.StartsWith(root, comparison);
        }


    }

}