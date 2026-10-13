
using System.Text;

using DProjects.Utils;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace DProjects.XShell.Middlewares {
    internal sealed class ResourcesMiddleware {


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

            // serve self-contained generated descriptors during development
            if (isDevelopment) {
                app.Use(async (context, nextMiddleware) => {
                    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)) {
                        await nextMiddleware();
                        return;
                    }
                    if (!context.Request.Path.StartsWithSegments(requestPath, out var remaining)) {
                        await nextMiddleware();
                        return;
                    }
                    var relativePath = remaining.Value ?? "";
                    if (!TryGetDescriptorKind(relativePath, out var descriptorName, out var descriptorKind)) {
                        await nextMiddleware();
                        return;
                    }

                    // resolve the requested descriptor directory without allowing traversal outside the resource root
                    var relativeFile = relativePath.TrimStart('/');
                    var requestedFile = Path.GetFullPath(Path.Combine(physicalPath, relativeFile.Replace('/', Path.DirectorySeparatorChar)));
                    var directory = Path.GetDirectoryName(requestedFile) ?? "";
                    if (!IsInside(directory, physicalPath)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    if (!Directory.Exists(directory)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    // use exactly one authored JSON or JSONC representation for either request extension
                    var descriptorPath = FindDescriptor(directory, descriptorName);
                    if (descriptorPath == null) {
                        await nextMiddleware();
                        return;
                    }
                    var json = await new Services.DescriptorInventory().CreateJsonAsync(descriptorPath, descriptorKind, context.RequestAborted);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    // return strict generated JSON for both JSON and JSONC request URLs
                    SetNoCacheHeaders(context.Response);
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength = bytes.Length;
                    if (!HttpMethods.IsHead(context.Request.Method)) await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                });
            }

            // compile module resources during development
            if (isDevelopment) {
                app.Use(async (context, nextMiddleware) => {
                    if (!context.Request.Path.StartsWithSegments(requestPath, out var remaining)) {
                        await nextMiddleware();
                        return;
                    }
                    var relativePath = remaining.Value ?? "";
                    if (!relativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && 
                        !relativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && 
                        !relativePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) {
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
                    if (extension.Equals(".js")) {
                        var htmlFile = Path.ChangeExtension(file, ".html");
                        var jsExists = File.Exists(file);
                        var htmlExists = File.Exists(htmlFile);
                        if (jsExists && htmlExists) {
                            throw new InvalidOperationException($"Conflicting development resources '{file}' and '{htmlFile}' both resolve to '{relativePath}'.");
                        }
                        if (!jsExists && htmlExists) {
                            file = htmlFile;
                        } else if (!jsExists) {
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }
                    } else if (extension.Equals(".html")) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    } else if (!File.Exists(file)) {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                    // search module.json or module.jsonc in the file's directory or its parent directories
                    string directory = Path.GetDirectoryName(file) ?? "";
                    string? moduleJson = null;
                    while (!string.IsNullOrEmpty(directory) && IsInside(directory, physicalPath)) {
                        var moduleJsonPath = Path.Combine(directory, "module.json");
                        var moduleJsoncPath = Path.Combine(directory, "module.jsonc");
                        if (File.Exists(moduleJsonPath) && File.Exists(moduleJsoncPath)) {
                            throw new InvalidOperationException($"Source '{directory}' contains both module.json and module.jsonc.");
                        } else if (File.Exists(moduleJsonPath)) {
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
                    var fileContent = await new Services.ModuleFileCompiler().CompileAsync(moduleJson, file, context.RequestAborted);
                    // return response
                    SetNoCacheHeaders(context.Response);
                    context.Response.ContentType = fileContent.ContentType;
                    await context.Response.WriteAsync(fileContent.Content);
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
        private static string? FindDescriptor(string directory, string name) {
            // reject ambiguous authored descriptors consistently with Pack
            var json = Path.Combine(directory, name + ".json");
            var jsonc = Path.Combine(directory, name + ".jsonc");
            if (File.Exists(json) && File.Exists(jsonc)) {
                throw new InvalidOperationException($"Source '{directory}' contains both {name}.json and {name}.jsonc.");
            }
            return File.Exists(json) ? json : File.Exists(jsonc) ? jsonc : null;
        }
        private static void SetNoCacheHeaders(HttpResponse response) {
            response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            response.Headers.Pragma = "no-cache";
            response.Headers.Expires = "0";
        }
        private static bool TryGetDescriptorKind(string relativePath, out string name, out Services.DescriptorKind kind) {
            // route all descriptor extensions through one enrichment path
            var fileName = relativePath[(relativePath.LastIndexOf('/') + 1)..];
            if (fileName.Equals("module.json", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("module.jsonc", StringComparison.OrdinalIgnoreCase)) {
                name = "module";
                kind = Services.DescriptorKind.Module;
                return true;
            }
            if (fileName.Equals("xshell.json", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("xshell.jsonc", StringComparison.OrdinalIgnoreCase)) {
                name = "xshell";
                kind = Services.DescriptorKind.XShell;
                return true;
            }
            name = "";
            kind = default;
            return false;
        }
        private static bool IsInside(string path, string root) {
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            root = Path.GetFullPath(root) .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            path = Path.GetFullPath(path) .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, comparison);
        }


        
    }
}
