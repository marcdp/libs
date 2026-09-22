using System.Text.Json;
using System.Security.Cryptography;

namespace DProjects.XShell.Services {

    public sealed class ModuleFilesIndexer {

        // consts
        public const string ModuleFilesJson = "module.files.json";

        // inner classes
        private sealed record FileIndexItem(string Path, long Size, string Hash);
        
        
        // methods
        public async Task<string> CreateJsonAsync(string path) {
            // scan
            var files = new List<FileIndexItem>();
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) {
                if (Path.GetFileName(file).Equals(ModuleFilesJson, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                await using var stream = File.OpenRead(file);
                var hash = await SHA256.HashDataAsync(stream);
                files.Add(new FileIndexItem(
                    "/" + Path.GetRelativePath(path, file).Replace('\\', '/'),
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