using System.Text.Json;
using System.Security.Cryptography;

namespace DProjects.XShell.Services {

    internal sealed class FilesIndexer {

        // consts
        public const string ModuleFilesJson = "module.files.json";

        // inner classes
        private sealed record FileIndexItem(string Path, long Size, string Hash);
        
        
        // methods
        public Task<string> CreateJsonAsync(string path) {
            return CreateJsonAsync(path, CancellationToken.None);
        }
        public async Task<string> CreateJsonAsync(string path, CancellationToken cancellationToken) {
            // scan
            var files = new List<FileIndexItem>();
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(path, file);
                if (relativePath.Equals(ModuleFilesJson, StringComparison.OrdinalIgnoreCase) ||
                    relativePath.Equals("module.json", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.Equals("xshell.json", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                await using var stream = File.OpenRead(file);
                var hash = await SHA256.HashDataAsync(stream, cancellationToken);
                files.Add(new FileIndexItem(
                    "/" + relativePath.Replace('\\', '/'),
                    new FileInfo(file).Length,
                    Convert.ToHexString(hash).ToLowerInvariant()
                ));
            }
            // sort
            files.Sort((a, b) => StringComparer.Ordinal.Compare(a.Path, b.Path));
            // return
            return JsonSerializer.Serialize(files, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            });
        }

    }
}
