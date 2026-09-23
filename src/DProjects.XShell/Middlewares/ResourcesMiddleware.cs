
using DProjects.Utils;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace DProjects.XShell.Middlewares {
    public sealed class ResourcesMiddleware {


        // fields
        private readonly RequestDelegate mPipeline;


        // ctor
        public ResourcesMiddleware(RequestDelegate next, IServiceProvider services, string physicalPath, string requestPath, bool isDevelopment) {
            physicalPath = Path.GetFullPath(physicalPath);
            requestPath = requestPath.TrimEnd('/');
            if (!Directory.Exists(physicalPath)) {
                throw new DirectoryNotFoundException($"Resources directory not found: {physicalPath}");
            }
            var fileProvider = new PhysicalFileProvider(physicalPath);

            // content types
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".jsonc"] = "application/json";
            contentTypeProvider.Mappings[".md"] = "text/markdown";

            // create inner middleware pipeline
            var app = new ApplicationBuilder(services);

            // virtual module.files.json
            if (isDevelopment) {
                app.Use(async (context, nextMiddleware) => {

                    if (!context.Request.Path.StartsWithSegments(requestPath, out var remaining)) {
                        await nextMiddleware();
                        return;
                    }
                    var relativePath = remaining.Value ?? "";
                    if (!relativePath.EndsWith("/" + Services.ModuleFilesIndexer.ModuleFilesJson, StringComparison.OrdinalIgnoreCase)) {
                        await nextMiddleware();
                        return;
                    }

                    // module.files.json represents its containing directory
                    var relativeDirectory = relativePath[..^("/" + Services.ModuleFilesIndexer.ModuleFilesJson).Length].TrimStart('/');
                    var directory = Path.GetFullPath(Path.Combine(physicalPath, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));

                    // prevent path traversal
                    if (!IsInside(directory, physicalPath)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    if (!Directory.Exists(directory)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    // only module directories expose module.files.json
                    var moduleJson = Path.Combine(directory, "module.json");
                    var moduleJsonc = Path.Combine(directory, "module.jsonc");
                    if (!File.Exists(moduleJson) && !File.Exists(moduleJsonc)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    // create index json
                    var json = await new Services.ModuleFilesIndexer().CreateJsonAsync(directory);

                    // return response
                    context.Response.ContentType = "application/json";
                    if (isDevelopment) SetNoCacheHeaders(context.Response);
                    await context.Response.WriteAsync(json);
                });
            }

            // compile files .js, .html and .md, to .js files
            if (isDevelopment) {
                app.Use(async (context, nextMiddleware) => {
                    if (!context.Request.Path.StartsWithSegments(requestPath, out var remaining)) {
                        await nextMiddleware();
                        return;
                    }
                    var relativePath = remaining.Value ?? "";
                    if (!relativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && 
                        !relativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && 
                        !relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) {
                        await nextMiddleware();
                        return;
                    }
                    // prepare
                    var relativeFile = relativePath.TrimStart('/');
                    var file = Path.GetFullPath(Path.Combine(physicalPath, relativeFile.Replace('/', Path.DirectorySeparatorChar)));
                    var extension = System.IO.Path.GetExtension(file);
                    // prevent path traversal
                    if (!IsInside(file, physicalPath)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    if (!File.Exists(file)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    // search module.json or module.jsonc in the file's directory or its parent directories
                    string directory = Path.GetDirectoryName(file) ?? "";
                    string? moduleJson = null;
                    while (!string.IsNullOrEmpty(directory) && IsInside(directory, physicalPath)) {
                        var moduleJsonPath = Path.Combine(directory, "module.json");
                        var moduleJsoncPath = Path.Combine(directory, "module.jsonc");
                        if (File.Exists(moduleJsonPath)) {
                            moduleJson = moduleJsonPath;
                            break;
                        } else if (File.Exists(moduleJsoncPath)) {
                            moduleJson = moduleJsoncPath;
                            break;
                        }
                        directory = Path.GetDirectoryName(directory) ?? "";
                    }
                    if (moduleJson == null) {
                        await nextMiddleware();
                        return;
                    }
                    // compile to js
                    var js = await new Services.ModuleFileCompiler().CompileToJsAsync(moduleJson, file);
                    // return response
                    context.Response.ContentType = "application/javascript";
                    SetNoCacheHeaders(context.Response);
                    await context.Response.WriteAsync(js);
                });
            }

            // default files
            app.UseDefaultFiles(new DefaultFilesOptions {
                FileProvider = fileProvider,
                RequestPath = requestPath
            });

            // static files
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = fileProvider,
                RequestPath = requestPath,
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = context => {
                    if (isDevelopment) {
                        SetNoCacheHeaders(context.Context.Response);
                    }
                }
            });

            // continue with outer application pipeline when not handled
            app.Run(next);

            // build inner pipeline
            mPipeline = app.Build();
        }


        // methods
        public Task InvokeAsync(HttpContext context) {
            return mPipeline(context);
        }
        private static void SetNoCacheHeaders(HttpResponse response) {
            response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";
        }
        private static bool IsInside(string path, string root) {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            root = Path.GetFullPath(root) .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path) .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, comparison);
        }


        
    }
}