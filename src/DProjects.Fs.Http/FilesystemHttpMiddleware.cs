
using System;
using DProjects.Factories;
using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace DProjects.Fs.Http {


    //handler
    public class FilesystemHttpMiddleware {

        //configuration
        public enum Modes {
            ReadWrite,
            ReadOnly,
            WriteOnly
        }
        public class Options {
            public string Url { get; set; }
            public string Path { get; set; }
            public string Prefix { get; set; }
            public Modes Mode { get; set; }
            public bool AllowAnonymous { get; set; }
            public bool AllowIfNotModifiedHeader { get; set; }
            public bool AllowDefaultDocument { get; set; }
            public Options(string path, string url) {
                Path = path;
                Url = url;
                Prefix = "";
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


        //constructor
        public FilesystemHttpMiddleware(RequestDelegate next, IFactoryByUrl<IFilesystem> fsFactory, Options options) {
            mNext = next;
            mOptions = options;
            mFilesystem = fsFactory.Create(options.Url);
        }


        //handle
        public async Task Invoke(HttpContext context, ILogger<IFilesystem> logger) {
            //check path
            if (!context.Request.Path.Value!.StartsWith(mOptions.Prefix)) {
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
            }
            //action
            try {
                var cancellationToken = context.RequestAborted;
                var method = context.Request.Method;
                var p = context.Request.Path.Value.Substring(mOptions.Prefix.Length);
                var path = BuildPath(p);
                context.Response.Headers.Append(FilesystemHttp.HEADER_FS_PATH_PREFIX, mOptions.Prefix);
                var accept = context.Request.Headers[HttpUtils.HEADER_ACCEPT];
                var ranges = context.Request.Headers[HttpUtils.HEADER_RANGE];
                if (method.Equals("GET")) {
                    //GET
                    var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
                    if (entry != null && entry.IsDirectory() && mOptions.AllowDefaultDocument) {
                        foreach (var defaultDocument in new string[] { "index.html" }) {
                            entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(path, defaultDocument), cancellationToken);
                            if (entry != null) {
                                if (context.Request.Path.Value.Length > 0 && !context.Request.Path.Value.EndsWith("/")) {
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
                        if (copyRequest != null && await mFilesystem.ExistsAsync(copyRequest.Source, cancellationToken)) {
                            var settings = new CopySettings();
                            settings.IgnoreErrors = copyRequest.IgnoreErrors;
                            settings.Overwrite = copyRequest.Overwrite;
                            settings.Recursive = copyRequest.Recursive;
                            settings.Tries = copyRequest.Tries;
                            await mFilesystem.CopyAsync(copyRequest.Source, path, settings, logger, cancellationToken);
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
                        if (moveRequest != null && await mFilesystem.ExistsAsync(moveRequest.Source, cancellationToken)) {
                            var settings = new MoveSettings();
                            settings.IgnoreErrors = moveRequest.IgnoreErrors;
                            await mFilesystem.MoveAsync(moveRequest.Source, path, settings, logger, cancellationToken);
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
                        if (syncRequest != null && await mFilesystem.ExistsAsync(syncRequest.Source, cancellationToken)) {
                            var syncSettings = new SyncSettings();
                            syncSettings.DestinationExcludes = syncRequest.DestinationExcludes;
                            syncSettings.SourceExcludes = syncRequest.SourceExcludes;
                            syncSettings.IgnoreErrors = syncRequest.IgnoreErrors;
                            syncSettings.Mode = syncRequest.Mode;
                            syncSettings.Tries = syncRequest.Tries;
                            await mFilesystem.SyncAsync(syncRequest.Source, path, syncSettings, logger, cancellationToken);
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
            var aux = path;
            if (aux.Length == 0) aux = "/";
            //aux = PathUtils.Combine(mOptions.Path, aux);
            aux = PathUtils.Uncombine(mOptions.Path, aux);
            return aux;
        }
        private string UnbuildPath(string path) {
            //var aux = mOptions.Path;
            //return PathUtils.Uncombine(aux, path);
            return path;
        }
         

    }
}