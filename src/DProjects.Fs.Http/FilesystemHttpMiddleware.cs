
using System;
using DProjects.Factories;
using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace DProjects.Fs.Http {


    //handler
    public class FilesystemHttpMiddleware : IDisposable {

        //configuration
        public enum Modes {
            ReadWrite,
            ReadOnly,
            WriteOnly
        }
        public class Options {
            public string FilesystemUrl { get; init; }
            public string FilesystemPath { get; init; } = "/";
            public string UrlPath { get; init; } = "";
            public Modes Mode { get; init; }
            public string AuthorizationPolicy { get; init; } = "";
            public bool AllowAnonymous { get; init; }
            public bool AllowIfNotModifiedHeader { get; init; }
            public bool AllowDefaultDocument { get; init; }
            public Options() {
                FilesystemUrl = "";
                Mode = Modes.ReadOnly;
                AllowAnonymous = false;
                AllowDefaultDocument = false;
                AllowIfNotModifiedHeader = true;
            }
            public Options(string path, string url) {
                FilesystemPath = path;
                FilesystemUrl = url;
                Mode = Modes.ReadOnly;
                AllowAnonymous = false;
                AllowDefaultDocument = false;
                AllowIfNotModifiedHeader = true;
            }
        }
         

        //variables
        private Options mOptions;
        private IFilesystem mFilesystem;
        private readonly RequestDelegate mNext;
        private readonly string mFilesystemPath;
        private readonly PathString mUrlPath;


        //constructor
        public FilesystemHttpMiddleware(RequestDelegate next, IFactoryByUrl<IFilesystem> fsFactory, Options options) {
            mNext = next;
            mOptions = options;
            mFilesystemPath = PathUtils.NormalizeStrict(options.FilesystemPath);
            mUrlPath = BuildUrlPath(options.UrlPath);
            mFilesystem = fsFactory.Create(options.FilesystemUrl);
        }
        public void Dispose() {
            mFilesystem.Dispose();
        }


        //handle
        public async Task Invoke(HttpContext context, ILogger<IFilesystem> logger) {
            //check path
            if (!TryGetRelativePath(context.Request.Path, out var relativePath)) {
                await mNext.Invoke(context);
                return;
            }
            //check auth
            if (!mOptions.AllowAnonymous) {
                if (context.User.Identity == null || !context.User.Identity.IsAuthenticated) {
                    context.Response.StatusCode = HttpUtils.HTTP_UNAUTHORIZED;
                    await context.Response.WriteAsync("");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(mOptions.AuthorizationPolicy)) {
                    var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                    var result = await authorizationService.AuthorizeAsync(context.User, context, mOptions.AuthorizationPolicy);
                    if (!result.Succeeded) {
                        context.Response.StatusCode = HttpUtils.HTTP_FORBIDDEN;
                        await context.Response.WriteAsync("");
                        return;
                    }
                }
            }
            //action
            try {
                var cancellationToken = context.RequestAborted;
                var method = context.Request.Method;
                var path = BuildPath(relativePath);
                context.Response.Headers.Append(FilesystemHttp.HEADER_FS_PATH_PREFIX, mUrlPath.Value ?? "");
                var accept = context.Request.Headers[HttpUtils.HEADER_ACCEPT];
                var ranges = context.Request.Headers[HttpUtils.HEADER_RANGE];
                if (method.Equals("GET")) {
                    //GET
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (entry != null && entry.IsDirectory() && mOptions.AllowDefaultDocument) {
                        foreach (var defaultDocument in new string[] { "index.html" }) {
                            entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(path, defaultDocument), cancellationToken);
                            if (entry != null) {
                                if (context.Request.Path.HasValue && !context.Request.Path.Value!.EndsWith("/")) {
                                    context.Response.Redirect(context.Request.Path.Value + "/");
                                    return;
                                }
                                break;
                            }
                        }
                    }
                    if (entry == null) {
                        //404
                        context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else if (accept.Count > 0 && accept[0] != null && accept[0]!.Equals(FilesystemHttp.MIMETYPE_FS_SUPPORTS)) {
                        //200 supports
                        if (Enum.TryParse<Features>(context.Request.Query["feature"].ToString(), true, out Features feature)) {
                            var result = await mFilesystem.SupportsAsync(path, feature, cancellationToken);
                            var json = System.Text.Json.JsonSerializer.Serialize(result);
                            await context.Response.WriteAsync(json);
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_BAD_REQUEST;
                            await context.Response.WriteAsync("");
                        }
                    } else if (accept.Count > 0 && accept[0]!.Equals(FilesystemHttp.MIMETYPE_FS_ENTRY)) {
                        //200 - MIMETYPE_FS_ENTRY
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_ENTRY;
                        var json = entry.ToJson(UnbuildPath(entry.Path));
                        await context.Response.WriteAsync(json);
                    } else if (accept.Count > 0 && accept[0]!.Equals(FilesystemHttp.MIMETYPE_FS_ENTRIES)) {
                        //200 - MIMETYPE_FS_ENTRIES
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_ENTRIES;
                        var mode = GetModes.All;
                        Enum.TryParse<GetModes>(context.Request.Query["mode"].ToString(), true, out mode);
                        var pattern = context.Request.Query["pattern"].ToString();
                        if (pattern.Length == 0) pattern = null;
                        await foreach (var childentry in mFilesystem.GetEntriesAsync(path, mode, pattern)) {
                            var json = childentry.ToJson(UnbuildPath(childentry.Path));
                            await context.Response.WriteAsync(json + "\n");
                        }
                    } else if ((accept.Count > 0 && accept[0]!.Equals(FilesystemHttp.MIMETYPE_FS_METADATA))) {
                        //200 - MIMETYPE_FS_METADATA
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_METADATA;
                        var metadata = await mFilesystem.GetMetadataAsync(path, cancellationToken);
                        var json = System.Text.Json.JsonSerializer.Serialize(metadata);
                        await context.Response.WriteAsync(json);
                    } else if (ranges.Count == 1 && ranges[0]!.StartsWith("bytes=")) {
                        //206 - Partial content
                        context.Response.StatusCode = HttpUtils.HTTP_PARTIAL_CONTENT;

                        context.Response.ContentType = MimeTypeUtils.GetMimeType(entry.Name);
                        if (MimeTypeUtils.IsText(context.Response.ContentType)) {
                            context.Response.ContentType += "; charset=utf-8";
                        }
                        var arr = ranges[0]!.Substring("bytes=".Length).Split('-');
                        Int64.TryParse(arr[0], out long offset);
                        Int64.TryParse(arr[1], out long to);
                        long length = (to == 0 ? -1 : to - offset + 1);
                        if (to == 0) {
                            context.Response.Headers[HttpUtils.HEADER_CONTENT_RANGE] = "bytes " + offset + "-" + (entry.Length - 1).ToString() + "/" + entry.Length.ToString();
                        } else {
                            context.Response.Headers[HttpUtils.HEADER_CONTENT_RANGE] = "bytes " + offset + "-" + to.ToString() + "/" + entry.Length.ToString();
                        }
                        using (var stream = await mFilesystem.LoadReadStreamAsync(entry.Path, new LoadReadStreamSettings() { Offset = offset, Length = length }, cancellationToken)) {
                            await stream.CopyToAsync(context.Response.Body);
                        }
                    } else if (entry.EntryType == EntryType.Directory) {
                        //200 - directory: no content
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        await context.Response.WriteAsync("");
                    } else {
                        //200
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        context.Response.ContentType = MimeTypeUtils.GetMimeType(entry.Name);
                        if (MimeTypeUtils.IsText(context.Response.ContentType)) {
                            context.Response.ContentType += "; charset=utf-8";
                        }
                        //if not modified
                        if (mOptions.AllowIfNotModifiedHeader) {
                            var ifModifiedSinceStr = context.Request.Headers[HttpUtils.HEADER_IF_MODIFIED_SINCE];
                            var lastModified = new DateTime(entry.Modified.Year, entry.Modified.Month, entry.Modified.Day, entry.Modified.Hour, entry.Modified.Minute, entry.Modified.Second);
                            DateTime.TryParse(ifModifiedSinceStr, out var ifModifiedSince);
                            context.Response.Headers[HttpUtils.HEADER_CACHE_CONTROL] = HttpUtils.CACHE_CONTROL_NO_CACHE;
                            context.Response.Headers[HttpUtils.HEADER_LAST_MODIFIED] = entry.Modified.ToUniversalTime().ToString("R");
                            if (ifModifiedSince != lastModified) {
                                using (var stream = await mFilesystem.LoadReadStreamAsync(entry.Path, new(), cancellationToken)) {
                                    await stream.CopyToAsync(context.Response.Body);
                                }
                            } else {
                                context.Response.StatusCode = HttpUtils.HTTP_NOT_MODIFIED;
                            }
                            return;
                        }
                        //return
                        using (var stream = await mFilesystem.LoadReadStreamAsync(entry.Path, new(), cancellationToken)) {
                            await stream.CopyToAsync(context.Response.Body);
                        }
                    }
                } else if (method.Equals("HEAD")) {
                    //HEAD
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (entry == null) {
                        //404
                        context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else {
                        //200 OK
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                        context.Response.ContentType = MimeTypeUtils.GetMimeType(entry.Name);
                        if (MimeTypeUtils.IsText(context.Response.ContentType)) {
                            context.Response.ContentType += "; charset=utf-8";
                        }
                        context.Response.ContentLength = entry.Length;
                    }
                } else if (method.Equals("OPTIONS")) {
                    //OPTIONS
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (entry == null) {
                        //404
                        context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else {
                        //204 No content 
                        var options = new List<string>();
                        options.Add("OPTIONS");
                        options.Add("GET");
                        options.Add("HEAD");
                        if (mOptions.Mode == Modes.ReadWrite || mOptions.Mode == Modes.WriteOnly) {
                            options.Add("PUT");
                            options.Add("POST");
                            options.Add("DELETE");
                            options.Add("PATCH");
                        }
                        context.Response.StatusCode = HttpUtils.HTTP_NO_CONTENT;
                        context.Response.Headers[HttpUtils.HEADER_ALLOW] = String.Join(",", options.ToArray());
                    }
                } else if (method.Equals("PUT")) {
                    //PUT
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    var contentType = context.Request.ContentType;
                    if (mOptions.Mode == Modes.ReadOnly) {
                        //readonly
                        context.Response.StatusCode = HttpUtils.HTTP_METHOD_NOT_ALLOWED;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else if (contentType != null && contentType.StartsWith(FilesystemHttp.MIMETYPE_FS_COPY)) {
                        //Copy
                        var copyRequest = await context.Request.ReadFromJsonAsync<DProjects.Fs.Http.FilesystemHttp.CopyRequest>(cancellationToken);
                        var sourcePath = copyRequest == null ? null : BuildHttpPath(copyRequest.Source);
                        if (copyRequest != null && sourcePath != null && await mFilesystem.ExistsAsync(sourcePath, cancellationToken)) {
                            var settings = new CopySettings();
                            settings.IgnoreErrors = copyRequest.IgnoreErrors;
                            settings.Overwrite = copyRequest.Overwrite;
                            settings.Recursive = copyRequest.Recursive;
                            settings.Tries = copyRequest.Tries;
                            await mFilesystem.CopyAsync(sourcePath, path, settings, logger, cancellationToken);
                            if (entry == null) {
                                context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                            } else {
                                context.Response.StatusCode = HttpUtils.HTTP_OK;
                            }
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                            await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                        }                        
                    } else if (contentType != null && contentType.StartsWith(FilesystemHttp.MIMETYPE_FS_MOVE)) {
                        //Move
                        var moveRequest = await context.Request.ReadFromJsonAsync<DProjects.Fs.Http.FilesystemHttp.MoveRequest>(cancellationToken);
                        var sourcePath = moveRequest == null ? null : BuildHttpPath(moveRequest.Source);
                        if (moveRequest != null && sourcePath != null && await mFilesystem.ExistsAsync(sourcePath, cancellationToken)) {
                            var settings = new MoveSettings();
                            settings.IgnoreErrors = moveRequest.IgnoreErrors;
                            await mFilesystem.MoveAsync(sourcePath, path, settings, logger, cancellationToken);
                            if (entry == null) {
                                context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                            } else {
                                context.Response.StatusCode = HttpUtils.HTTP_OK;
                            }
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                            await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                        }
                    } else if (contentType != null && contentType.StartsWith(FilesystemHttp.MIMETYPE_FS_SYNC)) {
                        //Sync
                        var syncRequest = await context.Request.ReadFromJsonAsync<DProjects.Fs.Http.FilesystemHttp.SyncRequest>(cancellationToken);
                        var sourcePath = syncRequest == null ? null : BuildHttpPath(syncRequest.Source);
                        if (syncRequest != null && sourcePath != null && await mFilesystem.ExistsAsync(sourcePath, cancellationToken)) {
                            var syncSettings = new SyncSettings();
                            syncSettings.DestinationExcludes = syncRequest.DestinationExcludes;
                            syncSettings.SourceExcludes = syncRequest.SourceExcludes;
                            syncSettings.IgnoreErrors = syncRequest.IgnoreErrors;
                            syncSettings.Mode = syncRequest.Mode;
                            syncSettings.Tries = syncRequest.Tries;
                            await mFilesystem.SyncAsync(sourcePath, path, syncSettings, logger, cancellationToken);
                            if (entry == null) {
                                context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                            } else {
                                context.Response.StatusCode = HttpUtils.HTTP_OK;
                            }
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                            await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                        }
                    } else if (contentType != null && contentType.StartsWith(FilesystemHttp.MIMETYPE_FS_ENTRY_DIRECTORY)) {
                        //CreateDirectory
                        if (entry == null) {
                            entry = await mFilesystem.CreateDirectoryAsync(path, cancellationToken);
                            context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_OK;
                        }
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_ENTRY;
                        var json = entry.ToJson(UnbuildPath(entry.Path));
                        await context.Response.WriteAsync(json);
                    } else if (contentType != null && contentType.StartsWith(FilesystemHttp.MIMETYPE_FS_APPEND)) {
                        //Append
                        if (entry == null) {
                            context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_OK;
                        }
                        entry = await mFilesystem.SaveFileAsync(path, context.Request.Body, new() {
                            Append = true,
                        }, cancellationToken);
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_ENTRY;
                        var json = entry.ToJson(UnbuildPath(entry.Path));
                        await context.Response.WriteAsync(json);
                    } else {
                        //SaveFile
                        if (entry == null) {
                            context.Response.StatusCode = HttpUtils.HTTP_CREATED;
                        } else {
                            context.Response.StatusCode = HttpUtils.HTTP_OK;
                        }
                        entry = await mFilesystem.SaveFileAsync(path, context.Request.Body, new(), cancellationToken);
                        context.Response.ContentType = FilesystemHttp.MIMETYPE_FS_ENTRY;
                        var json = entry.ToJson(UnbuildPath(entry.Path));
                        await context.Response.WriteAsync(json);
                    }
                } else if (method.Equals("DELETE")) {
                    //DELETE
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (mOptions.Mode == Modes.ReadOnly) {
                        //readonly
                        context.Response.StatusCode = HttpUtils.HTTP_METHOD_NOT_ALLOWED;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else if (entry == null) {
                        context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                    } else {
                        var type = context.Request.Headers[FilesystemHttp.HEADER_FS_IF_ENTRY_FILE];
                        if ("file".Equals(type)) {
                            await mFilesystem.DeleteFileAsync(path, cancellationToken);
                        } else if ("dir".Equals(type)) {
                            await mFilesystem.DeleteDirectoryAsync(path, cancellationToken);
                        } else {
                            await mFilesystem.DeleteAsync(path, cancellationToken);
                        }
                        context.Response.StatusCode = HttpUtils.HTTP_OK;
                    }
                } else if (method.Equals("PATCH")) {
                    //PATCH
                    var patch = await context.Request.ReadFromJsonAsync<DProjects.Fs.Http.FilesystemHttp.PatchRequest>(cancellationToken);
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (mOptions.Mode == Modes.ReadOnly) {
                        //readonly
                        context.Response.StatusCode = HttpUtils.HTTP_METHOD_NOT_ALLOWED;
                        await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                    } else if (entry == null) {
                        //not found
                        context.Response.StatusCode = HttpUtils.HTTP_NOT_FOUND;
                    } else {
                        if (patch == null) {
                            //bad request
                            context.Response.StatusCode = HttpUtils.HTTP_BAD_REQUEST;
                        } else if (patch.Metadata != null ) {
                            //metadata
                            await mFilesystem.SetMetadataAsync(path, patch.Metadata, cancellationToken);
                            context.Response.StatusCode = HttpUtils.HTTP_OK;
                            await context.Response.WriteAsync("");
                        } else if (patch.Modified != null) {
                            //touch
                            await mFilesystem.TouchAsync(path, patch.Modified ?? DateTime.Now, cancellationToken);
                            context.Response.StatusCode = HttpUtils.HTTP_OK;
                            await context.Response.WriteAsync("");
                        }
                    }
                } else {
                    //406 Not acceptable
                    context.Response.StatusCode = HttpUtils.HTTP_NOT_ACCEPTABLE;
                    await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
                }
            } catch (BadHttpRequestException e) {
                context.Response.StatusCode = e.StatusCode;
                await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
            } catch (UnauthorizedAccessException e) {
                logger.LogError("DshListenerHttpShareMiddleware: UnauthorizedAccessException: " + e.Message + " " + e.StackTrace);
                context.Response.StatusCode = HttpUtils.HTTP_UNAUTHORIZED;
                await context.Response.WriteAsync(HttpUtils.GetHttpCodeDescription(context.Response.StatusCode));
            } catch (Exception e) {
                context.Response.StatusCode = HttpUtils.HTTP_INTERNAL_SERVER_ERROR;
                await context.Response.WriteAsync(e.Message + "\n" + e.StackTrace);
                logger.LogError("DshListenerHttpShareMiddleware: Exceptionn: " + e.Message + " " + e.StackTrace);
            }
        }
        private string BuildPath(string path) {
            //validate decoded traversal without changing literal percent-encoded filesystem names
            string requestPath = path.Length == 0 ? "/" : path;
            try {
                PathUtils.NormalizeStrict(DecodePath(requestPath));
            } catch (ArgumentException e) {
                throw new BadHttpRequestException("Invalid filesystem request path.", HttpUtils.HTTP_BAD_REQUEST, e);
            }
            string relativePath = PathUtils.NormalizeStrict(requestPath);
            string filesystemPath = PathUtils.Combine(mFilesystemPath, relativePath);
            if (!PathUtils.IsWithin(filesystemPath, mFilesystemPath)) {
                throw new BadHttpRequestException("The request path is outside the configured filesystem path.", HttpUtils.HTTP_BAD_REQUEST);
            }
            return filesystemPath;
        }
        private string UnbuildPath(string path) {
            //return only paths contained by the configured filesystem root
            string filesystemPath = PathUtils.NormalizeStrict(path);
            if (!PathUtils.IsWithin(filesystemPath, mFilesystemPath)) {
                throw new InvalidOperationException("The filesystem returned a path outside the configured filesystem path.");
            }
            return PathUtils.Uncombine(mFilesystemPath, filesystemPath, StringComparison.Ordinal);
        }
        private static PathString BuildUrlPath(string path) {
            //normalize root URL paths to the empty prefix used to handle all requests
            if (path.Length == 0) return PathString.Empty;
            string normalizedPath = PathUtils.NormalizeStrict(path);
            return normalizedPath == "/" ? PathString.Empty : new PathString(normalizedPath);
        }
        private bool TryGetRelativePath(PathString path, out string relativePath) {
            //match the configured URL path by complete path segments
            if (!mUrlPath.HasValue) {
                relativePath = path.Value ?? "";
                return true;
            }
            if (path.StartsWithSegments(mUrlPath, out PathString remainingPath)) {
                relativePath = remainingPath.Value ?? "";
                return true;
            }
            relativePath = "";
            return false;
        }
        private string BuildHttpPath(string path) {
            //apply the same URL-prefix mapping to source paths supplied in request bodies
            if (string.IsNullOrEmpty(path) || !path.StartsWith("/", StringComparison.Ordinal)) {
                throw new BadHttpRequestException("The source path must be an absolute HTTP path.", HttpUtils.HTTP_BAD_REQUEST);
            }
            if (!TryGetRelativePath(new PathString(path), out string relativePath)) {
                throw new BadHttpRequestException("The source path is outside the configured URL path.", HttpUtils.HTTP_BAD_REQUEST);
            }
            return BuildPath(relativePath);
        }
        private static string DecodePath(string path) {
            //decode nested escaping so encoded traversal segments cannot reach a filesystem provider
            string decodedPath = path;
            for (int i = 0; i < 4; i++) {
                string unescapedPath = Uri.UnescapeDataString(decodedPath);
                if (unescapedPath == decodedPath) return decodedPath;
                decodedPath = unescapedPath;
            }
            if (Uri.UnescapeDataString(decodedPath) != decodedPath) {
                throw new BadHttpRequestException("The request path contains excessive URL encoding.", HttpUtils.HTTP_BAD_REQUEST);
            }
            return decodedPath;
        }
         

    }
}
