using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using System.Runtime.CompilerServices;

namespace DProjects.Repositories {

    public class GenericRepositoryFsDir<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : IGenericRepositoryElement<TKey> {


        //enum
        public enum Formats {
            Json,
            Yaml,
            Yfm
        }


        //vars
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly Formats mFormat;
         

        //ctor
        public GenericRepositoryFsDir(IFilesystem filesystem, string path, Formats format) {
            this.mFilesystem = filesystem;
            this.mPath = path;
            this.mFormat = format;
            if (!mFilesystem.ExistsDirectory(mPath)) mFilesystem.CreateDirectory(mPath);
        }


        //methods   
        public async Task AddAsync(TEntity element, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, element.Id + "." + mFormat.ToString().ToLower());
            var text = Serialize(element);
            await mFilesystem.SaveTextFileAsync(path, text, System.Text.Encoding.UTF8, cancellationToken);            
        }
        public async Task SaveAsync(TEntity element, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, element.Id + "." + mFormat.ToString().ToLower());
            var text = Serialize(element);
            await mFilesystem.SaveTextFileAsync(path, text, System.Text.Encoding.UTF8, cancellationToken);
        }
        public async Task<TEntity?> GetAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, id + "." + mFormat.ToString().ToLower());
            var entry = await mFilesystem.GetEntryAsync(path, cancellationToken);
            if (entry == null) return default;
            var text = await mFilesystem.LoadTextFileAsync(entry.Path, System.Text.Encoding.UTF8, cancellationToken);
            var result = Deserialize(text);
            return result;
        }

        public async IAsyncEnumerable<TEntity> ListAsync(string pattern, [EnumeratorCancellation] CancellationToken cancellationToken) {
            await foreach (var entry in mFilesystem.GetEntriesAsync(mPath, DProjects.Fs.GetModes.Files, pattern + "." + mFormat.ToString().ToLower())) {
                var text = await mFilesystem.LoadTextFileAsync(entry.Path, System.Text.Encoding.UTF8, cancellationToken);
                var result = Deserialize(text);
                yield return result;
            }
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, id + "." + mFormat.ToString().ToLower());
            return mFilesystem.DeleteFileAsync(path, cancellationToken);
        }


        //private 
        private string Serialize(TEntity element) {
            if (mFormat == Formats.Json) {
                return JsonSerializer.Serialize(element, new JsonSerializerOptions() {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
            } else if (mFormat == Formats.Yaml) {
                var serializer = new DProjects.Text.Yaml.YamlSerializer(new () { 
                    FrontMatter = false,
                });
                return serializer.Serialize(element);
            } else if (mFormat == Formats.Yfm) {
                var serializer = new DProjects.Text.Yaml.YamlSerializer(new() {
                    FrontMatter = true,
                    ContentPropertyNames = new[] { "content" },
                });
                return serializer.Serialize(element);
            }
            throw new NotImplementedException();
        }
        private TEntity Deserialize(string text) {
            if (mFormat == Formats.Json) {
                var result = JsonSerializer.Deserialize<TEntity>(text, new JsonSerializerOptions() {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                return result!;
            } else if (mFormat == Formats.Yaml) {
                var deserializer = new DProjects.Text.Yaml.YamlDeserializer(new() {
                    ExpectFrontMatter = false,
                    ContentNodes = false
                });
                var result = deserializer.Deserialize<TEntity>(text);
                return result;
            } else if (mFormat == Formats.Yfm) {
                var deserializer = new DProjects.Text.Yaml.YamlDeserializer(new() {
                    ExpectFrontMatter = true,
                    ContentNodes = true
                });
                var result = deserializer.Deserialize<TEntity>(text);
                return result;
            }
            throw new NotImplementedException();
        }
    }

}
