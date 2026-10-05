namespace DProjects.XShell.Services {

    public sealed class BoostrapFilesBuilder {

        public Dictionary<string, string> Build(Extensions.Configuration config, string environment) {
            var result = new Dictionary<string, string>();

            // index
            result["/index.html"] = $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>

                        <!-- general -->
                        <meta charset="utf-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        <meta http-equiv="Content-Security-Policy" content="{config.CSPValue}">

                        <!-- config xshell -->
                        <meta name="xshell:app.basePath"       content="{config.AppBasePath}">
                        <meta name="xshell:app.configPath"     content="{config.AppConfigPath}">
                        <meta name="xshell:app.params"         content="{string.Join("&", config.AppParams.Select(kv => kv.Key + "=" + kv.Value))}">
                        <meta name="xshell:xshell.environment" content="{(environment)}">
                        <meta name="xshell:xshell.temp.url"    content="{(config.TempUrl)}">

                        <!-- bootstrap xshell -->
                        <script src="{config.XShellBasePath}/bootstrap.js" ></script>

                    </head>
                    <body>
                    </body>
                    </html>
                    """;

            // sw.js
            result["/sw.js"] = $"""
                    // import real service worker script from xshell cdn
                    importScripts("{config.XShellBasePath}/sw.js"); 
                    """;

            // return
            return result;
        }
    }
}
