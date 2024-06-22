
using DProjects.Utils;
using DProjects.Fs;
using DProjects.Fs.Extensions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Fs.Test {



    public abstract class FilesystemTests : IDisposable {


        //vars
        protected IFactoryByUrl<IFilesystem> mFilesystemFactoryByUrl;
        protected IFilesystem mFilesystem;
        protected ILogger<IFilesystem> mLogger;
        protected string mPathPrefix;
        protected int mFolders; 
        protected int mFilesPerFolder;
        protected string mFileContent;


        //constructor
        public FilesystemTests(string url) {

            var services = new ServiceCollection();
            services.AddFactoryByUrl<IFilesystem>(cfg => {
                cfg.AddFactoriesFromAssembly<DProjects.Fs.Assembly>();
            });
            var serviceProvider = services.BuildServiceProvider();
            mFilesystemFactoryByUrl = serviceProvider.GetService<FactoryByUrl<IFilesystem>>()!;

            mFilesystem = mFilesystemFactoryByUrl.Create(url);
            mLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<IFilesystem>();
            mPathPrefix = "/test";
            mFileContent = "asdas asdfjaàaakkl adfñas asd fasdf sdLKA";
        }
        public virtual void Dispose() {
            (mFilesystem as IDisposable)?.Dispose();
        }


        //methods
        protected void CreateFilesystemStructure() {
            if (mFilesystem.ExistsDirectory(mPathPrefix)) {
                mFilesystem.DeleteDirectory(mPathPrefix);
            }
            mFilesystem.CreateDirectory(mPathPrefix);
            mFolders = 5;
            mFilesPerFolder = 3;
            for (int j = 0; j <= mFilesPerFolder - 1; j++) {
                mFilesystem.SaveTextFile(PathUtils.Combine("/test", "file" + j + ".txt"), mFileContent, System.Text.Encoding.UTF8);
            }
            for (int i = 0; i <= mFolders - 1; i++) {
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "folder" + i));
                for (int j = 0; j <= mFilesPerFolder - 1; j++) {
                    mFilesystem.SaveTextFile(PathUtils.Combine("/test", "folder" + i, "file" + j + ".txt"), mFileContent, System.Text.Encoding.UTF8);
                }
            }
        }
        protected async Task CreateFilesystemStructureAsync() {
            if (await mFilesystem.ExistsDirectoryAsync(mPathPrefix)) {
                await mFilesystem.DeleteDirectoryAsync(mPathPrefix, default);
            }
            await mFilesystem.CreateDirectoryAsync(mPathPrefix, default);
            mFolders = 5;
            mFilesPerFolder = 3;
            for (int j = 0; j <= mFilesPerFolder - 1; j++) {
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine("/test", "file" + j + ".txt"), mFileContent, System.Text.Encoding.UTF8);
            }
            for (int i = 0; i <= mFolders - 1; i++) {
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "folder" + i), default);
                for (int j = 0; j <= mFilesPerFolder - 1; j++) {
                    await mFilesystem.SaveTextFileAsync(PathUtils.Combine("/test", "folder" + i, "file" + j + ".txt"), mFileContent, System.Text.Encoding.UTF8);
                }
            }
        }


        //tests
        [Fact()]
        public virtual void GetEntries() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //check childs count at /test
                Assert.Equal(mFilesPerFolder + mFolders, new List<Entry>(mFilesystem.GetEntries(mPathPrefix)).Count);
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(mPathPrefix, GetModes.Files)).Count);
                Assert.Equal(mFolders, new List<Entry>(mFilesystem.GetEntries(mPathPrefix, GetModes.Directories)).Count);
                Assert.Equal(mFolders + mFilesPerFolder + mFolders * mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(mPathPrefix, GetModes.Descendants)).Count);
                //check childs count at /test/folder0
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(PathUtils.Combine(mPathPrefix, "folder0"))).Count);
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Files)).Count);
                Assert.Empty(new List<Entry>(mFilesystem.GetEntries(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Directories)));
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Descendants)).Count);
                //criteria
                Assert.Equal(mFilesPerFolder + mFolders * mFilesPerFolder, new List<Entry>(mFilesystem.GetEntries(mPathPrefix, GetModes.Descendants, "*.txt")).Count);
                Assert.Equal(mFolders + 1, new List<Entry>(mFilesystem.GetEntries(mPathPrefix, GetModes.Descendants, "file2*")).Count);
                //sort
                var aux = PathUtils.Combine(mPathPrefix, "order");
                mFilesystem.CreateDirectory(aux);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "Facturas"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "Facturas", "item1.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "Facturas FR"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "Facturas FR", "item2.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "zzzz"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "zzzz", "item1.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "zzzz.txt"), "hola mundo", System.Text.Encoding.UTF8);
                var entries = new List<Entry>(mFilesystem.GetEntries(aux, GetModes.All));
                Assert.Equal("Facturas", entries[0].Name);
                Assert.Equal("Facturas FR", entries[1].Name);
                entries = new List<Entry>(mFilesystem.GetEntries(aux, GetModes.Descendants));
                Assert.Equal("Facturas", entries[0].Name);
                Assert.Equal("item1.txt", entries[1].Name);
                Assert.Equal("Facturas FR", entries[2].Name);
                Assert.Equal("item2.txt", entries[3].Name);

                Assert.Equal("zzzz", entries[4].Name);
                Assert.Equal("item1.txt", entries[5].Name);
                Assert.Equal("zzzz.txt", entries[6].Name);
            }
            //get all entries
            mFilesystem.GetEntries("/");
        }
        [Fact()]
        public virtual async Task GetEntriesAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //check childs count at /test

                Assert.Equal(mFilesPerFolder + mFolders, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix).ToBlockingEnumerable()).Count);
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix, GetModes.Files).ToBlockingEnumerable()).Count);
                Assert.Equal(mFolders, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix, GetModes.Directories).ToBlockingEnumerable()).Count);
                Assert.Equal(mFolders + mFilesPerFolder + mFolders * mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix, GetModes.Descendants).ToBlockingEnumerable()).Count);
                //check childs count at /test/folder0
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(PathUtils.Combine(mPathPrefix, "folder0")).ToBlockingEnumerable()).Count);
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Files).ToBlockingEnumerable()).Count);
                Assert.Empty(new List<Entry>(mFilesystem.GetEntriesAsync(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Directories).ToBlockingEnumerable()));
                Assert.Equal(mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(PathUtils.Combine(mPathPrefix, "folder0"), GetModes.Descendants).ToBlockingEnumerable()).Count);
                //criteria
                Assert.Equal(mFilesPerFolder + mFolders * mFilesPerFolder, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix, GetModes.Descendants, "*.txt").ToBlockingEnumerable()).Count);
                Assert.Equal(mFolders + 1, new List<Entry>(mFilesystem.GetEntriesAsync(mPathPrefix, GetModes.Descendants, "file2*").ToBlockingEnumerable()).Count);
                //sort
                var aux = PathUtils.Combine(mPathPrefix, "order");
                mFilesystem.CreateDirectory(aux);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "Facturas"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "Facturas", "item1.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "Facturas FR"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "Facturas FR", "item2.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(aux, "zzzz"));
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "zzzz", "item1.txt"), "hola mundo", System.Text.Encoding.UTF8);
                mFilesystem.SaveTextFile(PathUtils.Combine(aux, "zzzz.txt"), "hola mundo", System.Text.Encoding.UTF8);
                var entries = new List<Entry>(mFilesystem.GetEntriesAsync(aux, GetModes.All).ToBlockingEnumerable());
                Assert.Equal("Facturas", entries[0].Name);
                Assert.Equal("Facturas FR", entries[1].Name);
                entries = new List<Entry>(mFilesystem.GetEntriesAsync(aux, GetModes.Descendants).ToBlockingEnumerable());
                Assert.Equal("Facturas", entries[0].Name);
                Assert.Equal("item1.txt", entries[1].Name);
                Assert.Equal("Facturas FR", entries[2].Name);
                Assert.Equal("item2.txt", entries[3].Name);

                Assert.Equal("zzzz", entries[4].Name);
                Assert.Equal("item1.txt", entries[5].Name);
                Assert.Equal("zzzz.txt", entries[6].Name);
            }
            //get all entries
            mFilesystem.GetEntries("/");
        }
        [Fact()]
        public virtual void GetEntry() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //check /test
                var entry = mFilesystem.GetEntry(mPathPrefix);
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, mPathPrefix);
                Assert.Equal(0, entry.Length);
                Assert.True(entry.IsDirectory());
                Assert.Empty(entry.Etag);
                Assert.Equal(entry.Name, PathUtils.GetPathName(mPathPrefix));
                //check /test/folder1
                entry = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "folder0"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "folder0"));
                Assert.Equal(0, entry.Length);
                Assert.True(entry.IsDirectory());
                Assert.Empty(entry.Etag);
                Assert.Equal("folder0", entry.Name);
                //check /test/file0
                entry = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file0.txt"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file0.txt"));
                Assert.Equal(entry.Length, System.Text.Encoding.UTF8.GetBytes(mFileContent).Length);
                Assert.False(entry.IsDirectory());
                Assert.NotEmpty(entry.Etag);
                Assert.Equal("file0.txt", entry.Name);
                //check /test/folder0/file0
                entry = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(PathUtils.Combine(mPathPrefix, "folder0"), "file0.txt"));
                Assert.Equal(entry.Length, System.Text.Encoding.UTF8.GetBytes(mFileContent).Length);
                Assert.False(entry.IsDirectory());
                Assert.NotEmpty(entry.Etag);
                Assert.Equal("file0.txt", entry.Name);
                //check get unexistent file /test/xxxxx
                entry = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, System.Guid.NewGuid().ToString()));
                Assert.Null(entry);
                //check get unexistent file /test/folder0/xxxxx
                entry = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "folder0", Guid.NewGuid().ToString()));
                Assert.Null(entry);
            }
            //get root entry
            var objRootEntry = mFilesystem.GetEntry("/");
            Assert.NotNull(objRootEntry);
            Assert.Equal("/", objRootEntry.Path);
            Assert.Equal(0, objRootEntry.Length);
            Assert.True(objRootEntry.IsDirectory());
        }
        [Fact()]
        public virtual async Task GetEntryAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //check /test
                var entry = await mFilesystem.GetEntryAsync(mPathPrefix);
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, mPathPrefix);
                Assert.Equal(0, entry.Length);
                Assert.True(entry.IsDirectory());
                Assert.Empty(entry.Etag);
                Assert.Equal(entry.Name, PathUtils.GetPathName(mPathPrefix));
                //check /test/folder1
                entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "folder0"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "folder0"));
                Assert.Equal(0, entry.Length);
                Assert.True(entry.IsDirectory());
                Assert.Empty(entry.Etag);
                Assert.Equal("folder0", entry.Name);
                //check /test/file0
                entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "file0.txt"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file0.txt"));
                Assert.Equal(entry.Length, System.Text.Encoding.UTF8.GetBytes(mFileContent).Length);
                Assert.False(entry.IsDirectory());
                Assert.NotEmpty(entry.Etag);
                Assert.Equal("file0.txt", entry.Name);
                //check /test/folder0/file0
                entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt"));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(PathUtils.Combine(mPathPrefix, "folder0"), "file0.txt"));
                Assert.Equal(entry.Length, System.Text.Encoding.UTF8.GetBytes(mFileContent).Length);
                Assert.False(entry.IsDirectory());
                Assert.NotEmpty(entry.Etag);
                Assert.Equal("file0.txt", entry.Name);
                //check get unexistent file /test/xxxxx
                entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, System.Guid.NewGuid().ToString()));
                Assert.Null(entry);
                //check get unexistent file /test/folder0/xxxxx
                entry = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "folder0", Guid.NewGuid().ToString()));
                Assert.Null(entry);
            }
            //get root entry
            var objRootEntry = await mFilesystem.GetEntryAsync("/");
            Assert.NotNull(objRootEntry);
            Assert.Equal("/", objRootEntry.Path);
            Assert.Equal(0, objRootEntry.Length);
            Assert.True(objRootEntry.IsDirectory());
        }
        [Fact()]
        public virtual void Exists() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //check exists /test
                Assert.True(mFilesystem.Exists(mPathPrefix));
                Assert.True(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.True(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                //exists
                Assert.True(mFilesystem.ExistsDirectory(mPathPrefix));
                Assert.False(mFilesystem.ExistsFile(mPathPrefix));
                Assert.True(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.False(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                Assert.False(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                //check unexists /test
                Assert.False(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString())));
                Assert.False(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "folder0", Guid.NewGuid().ToString())));
                Assert.False(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt", Guid.NewGuid().ToString())));
                Assert.False(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString(), "folder0")));
                Assert.False(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString(), "file0.txt")));
                //cre
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "/a"));
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "/a/b"));
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "/a/b/c"));
                Assert.True(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "/a")));
                Assert.True(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "/a/b")));
                Assert.True(mFilesystem.Exists(PathUtils.Combine(mPathPrefix, "/a/b/c")));
            }
            //check root
            Assert.True(mFilesystem.Exists("/"));
            Assert.False(mFilesystem.ExistsFile("/" + Guid.NewGuid().ToString()));
        }
        [Fact()]
        public virtual async Task ExistsAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //check exists /test
                Assert.True(await mFilesystem.ExistsAsync(mPathPrefix));
                Assert.True(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.True(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                //exists
                Assert.True(await mFilesystem.ExistsDirectoryAsync(mPathPrefix));
                Assert.False(await mFilesystem.ExistsFileAsync(mPathPrefix));
                Assert.True(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.False(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, "folder0")));
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                Assert.False(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt")));
                //check unexists /test
                Assert.False(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString())));
                Assert.False(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "folder0", Guid.NewGuid().ToString())));
                Assert.False(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "folder0", "file0.txt", Guid.NewGuid().ToString())));
                Assert.False(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString(), "folder0")));
                Assert.False(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString(), "file0.txt")));
                //cre
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "/a"), default);
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "/a/b"), default);
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "/a/b/c"), default);
                Assert.True(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "/a")));
                Assert.True(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "/a/b")));
                Assert.True(await mFilesystem.ExistsAsync(PathUtils.Combine(mPathPrefix, "/a/b/c")));
            }
            //check root
            Assert.True(await mFilesystem.ExistsAsync("/"));
            Assert.False(await mFilesystem.ExistsFileAsync("/" + Guid.NewGuid().ToString()));
        }
        [Fact()]
        public virtual void LoadReadStream() {
            //load read stream from root folder (should raise exception)
            try {
                mFilesystem.LoadBinaryFile("/");
                Assert.True(false);
            } catch (Exception) {
            }
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //load stream from folder (should raise exception)
                try {
                    mFilesystem.LoadBinaryFile(mPathPrefix);
                    Assert.True(false);
                } catch (Exception) {
                }
                //check loadstream /test/file0.txt
                using (Stream objReadStream = mFilesystem.LoadReadStream(PathUtils.Combine(mPathPrefix, "file0.txt"))) {
                    string aux = StreamUtils.ReadText(objReadStream, System.Text.Encoding.UTF8);
                    Assert.Equal(aux, mFileContent);
                }

                //load stream from unexistent file (should raise exception)
                try {
                    mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString()));
                    Assert.True(false);
                } catch (Exception) {
                }
                //load partial file
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "file0.txt"), "hola que tal estas.", System.Text.Encoding.ASCII);
                using (Stream objStream = mFilesystem.LoadReadStream(PathUtils.Combine(mPathPrefix, "file0.txt"), new LoadReadStreamSettings() { Offset = 5, Length = -1 })) {
                    byte[] bytes = StreamUtils.ReadBytes(objStream);
                    var aux = System.Text.Encoding.ASCII.GetString(bytes);
                    Assert.Equal("que tal estas.", aux);
                }
                using (Stream objStream = mFilesystem.LoadReadStream(PathUtils.Combine(mPathPrefix, "file0.txt"), new LoadReadStreamSettings() { Offset = 5, Length = 3 })) {
                    byte[] bytes = StreamUtils.ReadBytes(objStream);
                    var aux = System.Text.Encoding.ASCII.GetString(bytes);
                    Assert.Equal("que", aux);
                }
            } else {
                foreach (Entry objChild in mFilesystem.GetEntries("/", GetModes.Files)) {
                    using (Stream objReadStream = mFilesystem.LoadReadStream(objChild.Path)) {
                        StreamUtils.Consume(objReadStream);
                    }
                    break;
                }
            }
        }
        [Fact()]
        public virtual async Task LoadReadStreamAsync() {
            //load read stream from root folder (should raise exception)
            try {
                await mFilesystem.LoadBinaryFileAsync("/");
                Assert.True(false);
            } catch (Exception) {
            }
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //load stream from folder (should raise exception)
                try {
                    await mFilesystem.LoadBinaryFileAsync(mPathPrefix);
                    Assert.True(false);
                } catch (Exception) {
                }
                //check loadstream /test/file0.txt
                using (var readStream = await mFilesystem.LoadReadStreamAsync(PathUtils.Combine(mPathPrefix, "file0.txt"))) {
                    string aux = await StreamUtils.ReadTextAsync(readStream, System.Text.Encoding.UTF8);
                    Assert.Equal(aux, mFileContent);
                }

                //load stream from unexistent file (should raise exception)
                try {
                    await mFilesystem.LoadBinaryFileAsync(PathUtils.Combine(mPathPrefix, Guid.NewGuid().ToString()));
                    Assert.True(false);
                } catch (Exception) {
                }
                //load partial file
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), "hola que tal estas.", System.Text.Encoding.ASCII);
                using (var stream = await mFilesystem.LoadReadStreamAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), new LoadReadStreamSettings() { Offset = 5, Length = -1 })) {
                    byte[] bytes = await StreamUtils.ReadBytesAsync(stream, default);
                    var aux = System.Text.Encoding.ASCII.GetString(bytes);
                    Assert.Equal("que tal estas.", aux);
                }
                using (var stream = await mFilesystem.LoadReadStreamAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), new LoadReadStreamSettings() { Offset = 5, Length = 3 })) {
                    byte[] bytes = await StreamUtils.ReadBytesAsync(stream, default);
                    var aux = System.Text.Encoding.ASCII.GetString(bytes);
                    Assert.Equal("que", aux);
                }
            } else {
                foreach (Entry objChild in mFilesystem.GetEntries("/", GetModes.Files)) {
                    using (var stream = await mFilesystem.LoadReadStreamAsync(objChild.Path)) {
                        await StreamUtils.ConsumeAsync(stream);
                    }
                    break;
                }
            }
        }
        [Fact()]
        public virtual void LoadWriteStream() {
            //load write stream from root folder (should raise exception)
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //LoadWriteStream should create file it it not exist
                string key = System.Guid.NewGuid().ToString();
                var text = new StringBuilder();
                while (text.Length < 10){//64 * 1024 + 10) {
                    text.Append("hòla marcus" + System.Guid.NewGuid().ToString());
                }
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text.ToString());
                using (var stream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key))) {
                    stream.Write(buffer, 0, buffer.Length);
                }
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, key)));
                Assert.Equal(buffer.Length, mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, key))!.Length);

                var aux = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(text.ToString(), aux);

                //append
                var appendBuffer = System.Text.Encoding.UTF8.GetBytes("hello");
                text.Append("hello");
                using (var stream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key), new() {  Append = true })) {
                    stream.Write(appendBuffer, 0, appendBuffer.Length);
                }
                aux = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(text.ToString(), aux);

                //truncate
                using (var stream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key), new() { Truncate = true })) {
                    stream.Write(appendBuffer, 0, appendBuffer.Length);
                }
                aux = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), aux);

                //append should create file if not exists
                using (var stream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key) + "A", new() { Append = true })) {
                    stream.Write(appendBuffer, 0, appendBuffer.Length);
                }
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key) + "A") );

                //truncate should create file if not exists
                appendBuffer[0] = 12;
                using (var stream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key) + "A", new() { Truncate = true })) {
                    stream.Write(appendBuffer, 0, appendBuffer.Length);
                }
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key) + "A"));
            }
        }
        [Fact()]
        public virtual async Task LoadWriteStreamAsync() {
            //load write stream from root folder (should raise exception)
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //LoadWriteStream should create file it it not exist
                string key = System.Guid.NewGuid().ToString();
                var text = new StringBuilder();
                while (text.Length < 64 * 1024 + 10) {
                    text.Append("hòla marcus" + System.Guid.NewGuid().ToString());
                }
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text.ToString());
                using (var stream = await mFilesystem.LoadWriteStreamAsync(PathUtils.Combine(mPathPrefix, key))) {
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, key)));
                Assert.Equal(buffer.Length, (await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, key))).Length);

                var aux = await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(text.ToString(), aux);

                //append
                var appendBuffer = System.Text.Encoding.UTF8.GetBytes("hello");
                text.Append("hello");
                using (Stream objStream = await mFilesystem.LoadWriteStreamAsync(PathUtils.Combine(mPathPrefix, key), new() { Append = true })) {
                    await objStream.WriteAsync(appendBuffer, 0, appendBuffer.Length);
                }
                aux = await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(text.ToString(), aux);

                //truncate
                using (Stream objStream = mFilesystem.LoadWriteStream(PathUtils.Combine(mPathPrefix, key), new() { Truncate = true })) {
                    objStream.Write(appendBuffer, 0, appendBuffer.Length);
                }
                aux = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, key));
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), aux);

                //append should create file if not exists
                using (var stream = await mFilesystem.LoadWriteStreamAsync(PathUtils.Combine(mPathPrefix, key) + "A", new() { Append = true })) {
                    await stream.WriteAsync(appendBuffer, 0, appendBuffer.Length);
                }
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix, key) + "A"));

                //truncate should create file if not exists
                appendBuffer[0] = 12;
                using (var stream = await mFilesystem.LoadWriteStreamAsync(PathUtils.Combine(mPathPrefix, key) + "A", new() { Truncate = true })) {
                    await stream.WriteAsync(appendBuffer, 0, appendBuffer.Length);
                }
                Assert.Equal(System.Text.Encoding.UTF8.GetString(appendBuffer), await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix, key) + "A"));

            }
        }
        [Fact()]
        public virtual void Supports() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //check touch
                mFilesystem.Supports("/", Features.Touch);
            }
        }
        [Fact()]
        public virtual async Task SupportsAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //check touch
                await mFilesystem.SupportsAsync("/", Features.Touch, default);
            }
        }
        [Fact()]
        public virtual void CreateDirectory() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                //create directory
                string key = System.Guid.NewGuid().ToString();
                Entry entry = mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, key));
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
                Assert.Equal("", entry.Etag);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, key));
                Assert.True(entry.IsDirectory());
                Assert.True(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key)));
                mFilesystem.DeleteDirectory(PathUtils.Combine(mPathPrefix, key));
                Assert.False(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key)));
                //create directory with invalid parent, should create folder
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, key, key, key));
                Assert.True(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key, key, key)));
                Assert.True(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key, key)));
                Assert.True(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key)));
                mFilesystem.DeleteDirectory(PathUtils.Combine(mPathPrefix, key, key, key));
                Assert.False(mFilesystem.ExistsDirectory(PathUtils.Combine(mPathPrefix, key, key, key)));
                //invalid characters in name should return exception
                foreach (char invalidCharacter in PathUtils.PATH_INVALID_CHARS) {
                    Assert.ThrowsAny<Exception>(() => {
                        entry = mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "direab" + invalidCharacter + ""));
                    });
                }
            }
        }
        [Fact()]
        public virtual async Task CreateDirectoryAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                //create directory
                string key = System.Guid.NewGuid().ToString();
                Entry entry = await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, key), default);
                Assert.NotNull(entry);
                Assert.Equal(0, entry.Length);
                Assert.Equal("", entry.Etag);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, key));
                Assert.True(entry.IsDirectory());
                Assert.True(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key)));
                await mFilesystem.DeleteDirectoryAsync(PathUtils.Combine(mPathPrefix, key), default);
                Assert.False(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key)));
                //create directory with invalid parent, should create folder
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, key, key, key), default);
                Assert.True(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key, key, key)));
                Assert.True(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key, key)));
                Assert.True(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key)));
                await mFilesystem.DeleteDirectoryAsync(PathUtils.Combine(mPathPrefix, key, key, key), default);
                Assert.False(await mFilesystem.ExistsDirectoryAsync(PathUtils.Combine(mPathPrefix, key, key, key)));
                //invalid characters in name should return exception
                foreach (char invalidCharacter in PathUtils.PATH_INVALID_CHARS) {
                    await Assert.ThrowsAnyAsync<Exception>(async () => {
                        entry = await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "direab" + invalidCharacter + ""), default);
                    });
                }
            }
        }
        [Fact()]
        public virtual void SaveFile() {
            if (!mFilesystem.IsReadonly) {
                //save file
                mFilesystem.CreateDirectory(mPathPrefix);
                Entry entry = mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(5, entry.Length);
                byte[] buffer = mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                string text = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.Equal("hola1", text);
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                //dates
                var now = DateTime.Now;
                var entry1 = mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                var entry2 = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(entry1.Modified, entry2.Modified);
                Assert.Equal(entry1.Modified, entry1.Modified);
                Assert.Equal(entry2.Modified, entry2.Modified);
                Assert.True(entry2.Modified.Subtract(now).TotalMinutes < 10);
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                //save big file
                var bigMs = new MemoryStream();
                var length = 1024 * 1024 * 60; //60 Mb
                var random = new System.Random();
                for (var i = 0; i < length; i++) bigMs.WriteByte((byte)random.Next(0,255));
                bigMs.Seek(0, SeekOrigin.Begin);
                var entryBig = mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin"), bigMs);
                Assert.Equal(bigMs.Length, entryBig.Length);
                using (var md5 = MD5.Create()) {
                    bigMs.Seek(0, SeekOrigin.Begin);
                    var hash1 = BitConverter.ToString(md5.ComputeHash(bigMs)).ToLower().Replace("-", "");
                    using(var bigStreamReaded = mFilesystem.LoadReadStream(PathUtils.Combine(mPathPrefix, "file1.bin"))) {
                        var hash2 = BitConverter.ToString(md5.ComputeHash(bigStreamReaded)).ToLower().Replace("-", "");
                        Assert.Equal(hash1, hash2);
                    }
                }
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));               
                //save file over unexistent file should create it
                mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                text = System.Text.Encoding.UTF8.GetString(mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, "file1.bin")));
                Assert.Equal("hola1", text);
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                //save file over unexistent directory should fail
                //mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "unexistent"));
                Assert.ThrowsAny<Exception>(() => {
                    mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "unexistent", "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                });
                //text file
                entry = mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin.txt"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin.txt"));
                Assert.Equal(5, entry.Length);
                //non ascii chars in name
                foreach (char nonAsciiChar in new char[] { 'à', 'Ñ', 'ç', Char.ConvertFromUtf32(0x4EB0).ToCharArray()[0] }) {
                    string p = PathUtils.Combine(mPathPrefix, "fileeee" + nonAsciiChar + ".txt");
                    entry = mFilesystem.SaveFile(p, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    Assert.True(mFilesystem.Exists(p));
                    Assert.Equal("hola1", mFilesystem.LoadTextFile(p));
                }
                //especial ascii chars in name
                foreach (string test in new String[] { "+", "&", "@", "=", ";", ",", "%", "#", "Ñ", "'", "(", ")", "[", "]", "`", "{", "}", "~", "%20" }) {
                    string p = PathUtils.Combine(mPathPrefix, "fileeee" + test + ".txt");
                    entry = mFilesystem.SaveFile(p, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    Assert.True(mFilesystem.Exists(p));
                    Assert.Equal("hola1", mFilesystem.LoadTextFile(p));
                    var bFound = false;
                    var subentries = new List<Entry>(mFilesystem.GetEntries(PathUtils.GetPathParent(p)));
                    foreach (var subentry in subentries) {
                        if (subentry.Path.Equals(p)) {
                            bFound = true;
                        }
                    }
                    Assert.True(bFound, "filename not found: " + test);
                }
                //invalid characters in name should return exception
                foreach (char invalidCharacter in PathUtils.PATH_INVALID_CHARS) {
                    Assert.ThrowsAny<Exception>(() => {
                        entry = mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "fileeab" + invalidCharacter + ".txt"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    });
                }
            }
        }
        [Fact()]
        public virtual async Task SaveFileAsync() {
            if (!mFilesystem.IsReadonly) {
                //save file
                await mFilesystem.CreateDirectoryAsync(mPathPrefix, default);
                Entry entry = await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(5, entry.Length);
                byte[] buffer = await mFilesystem.LoadBinaryFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"));
                string text = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.Equal("hola1", text);
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
                //dates
                var now = DateTime.Now;
                var entry1 = await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                var entry2 = await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(entry1.Modified, entry2.Modified);
                Assert.Equal(entry1.Modified, entry1.Modified);
                Assert.Equal(entry2.Modified, entry2.Modified);
                Assert.True(entry2.Modified.Subtract(now).TotalMinutes < 10);
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
                //save big file
                var bigMs = new MemoryStream();
                var length = 1024 * 1024 * 10; //10 Mb
                var random = new System.Random();
                for (var i = 0; i < length; i++) bigMs.WriteByte((byte)random.Next(0, 255));
                bigMs.Seek(0, SeekOrigin.Begin);
                var entryBig = await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), bigMs);
                Assert.Equal(bigMs.Length, entryBig.Length);
                using (var md5 = MD5.Create()) {
                    bigMs.Seek(0, SeekOrigin.Begin);
                    var hash1 = BitConverter.ToString(md5.ComputeHash(bigMs)).ToLower().Replace("-", "");
                    using (var bigStreamReaded = await mFilesystem.LoadReadStreamAsync(PathUtils.Combine(mPathPrefix, "file1.bin"))) {
                        var hash2 = BitConverter.ToString(md5.ComputeHash(bigStreamReaded)).ToLower().Replace("-", "");
                        Assert.Equal(hash1, hash2);
                    }
                }
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
                //save file over unexistent file should create it
                await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                text = System.Text.Encoding.UTF8.GetString(mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, "file1.bin")));
                Assert.Equal("hola1", text);
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
                //save file over unexistent directory should fail
                //mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "unexistent"));
                await Assert.ThrowsAnyAsync<Exception>(async () => {
                    await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "unexistent", "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                });
                //text file
                entry = await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin.txt"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin.txt"));
                Assert.Equal(5, entry.Length);
                //non ascii chars in name
                foreach (char nonAsciiChar in new char[] { 'à', 'Ñ', 'ç', Char.ConvertFromUtf32(0x4EB0).ToCharArray()[0] }) {
                    string p = PathUtils.Combine(mPathPrefix, "fileeee" + nonAsciiChar + ".txt");
                    entry = await mFilesystem.SaveFileAsync(p, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    Assert.True(await mFilesystem.ExistsAsync(p));
                    Assert.Equal("hola1", await mFilesystem.LoadTextFileAsync(p));
                }
                //especial ascii chars in name
                foreach (string test in new String[] { "+", "&", "@", "=", ";", ",", "%", "#", "Ñ", "'", "(", ")", "[", "]", "`", "{", "}", "~", "%20" }) {
                    string p = PathUtils.Combine(mPathPrefix, "fileeee" + test + ".txt");
                    entry = await mFilesystem.SaveFileAsync(p, new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    Assert.True(await mFilesystem.ExistsAsync(p));
                    Assert.Equal("hola1", await mFilesystem.LoadTextFileAsync(p));
                    var bFound = false;
                    var subentries = new List<Entry>(mFilesystem.GetEntries(PathUtils.GetPathParent(p)));
                    foreach (var subentry in subentries) {
                        if (subentry.Path.Equals(p)) {
                            bFound = true;
                        }
                    }
                    Assert.True(bFound, "filename not found: " + test);
                }
                //invalid characters in name should return exception
                foreach (char invalidCharacter in PathUtils.PATH_INVALID_CHARS) {
                    await Assert.ThrowsAnyAsync<Exception>(async() => {
                        entry = await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "fileeab" + invalidCharacter + ".txt"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                    });
                }
            }
        }
        [Fact()]
        public virtual void AppendFile() {
            if (!mFilesystem.IsReadonly) {
                //append file
                mFilesystem.CreateDirectory(mPathPrefix);
                mFilesystem.SaveFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Entry entry = mFilesystem.AppendFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola2")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(10, entry.Length);
                byte[] buffer = mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                string text = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.Equal("hola1hola2", text);
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
                //append file over unexistent file should create it
                mFilesystem.AppendFile(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                text = System.Text.Encoding.UTF8.GetString(mFilesystem.LoadBinaryFile(PathUtils.Combine(mPathPrefix, "file1.bin")));
                Assert.Equal("hola1", text);
                mFilesystem.DeleteFile(PathUtils.Combine(mPathPrefix, "file1.bin"));
            }
        }
        [Fact()]
        public virtual async Task AppendFileAsync() {
            if (!mFilesystem.IsReadonly) {
                //append file
                await mFilesystem.CreateDirectoryAsync(mPathPrefix, default);
                await mFilesystem.SaveFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                Entry entry = await mFilesystem.AppendFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola2")));
                Assert.NotNull(entry);
                Assert.Equal(entry.Path, PathUtils.Combine(mPathPrefix, "file1.bin"));
                Assert.Equal(10, entry.Length);
                byte[] buffer = await mFilesystem.LoadBinaryFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"));
                string text = System.Text.Encoding.UTF8.GetString(buffer);
                Assert.Equal("hola1hola2", text);
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
                //append file over unexistent file should create it
                await mFilesystem.AppendFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hola1")));
                text = System.Text.Encoding.UTF8.GetString(await mFilesystem.LoadBinaryFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin")));
                Assert.Equal("hola1", text);
                await mFilesystem.DeleteFileAsync(PathUtils.Combine(mPathPrefix, "file1.bin"), default);
            }
        }
        [Fact()]
        public virtual void Copy() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                var settings = new CopySettings();
                //copy directory (should copy nothing)
                settings = new CopySettings();
                settings.Recursive = false;
                settings.Overwrite = true;
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "1111"));
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "2222"));
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "1111"), PathUtils.Combine(mPathPrefix, "2222"), settings, mLogger);
                Assert.False(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, "2222", "file0.txt2")));
                //copy directory
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "1111"));
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "2222"));
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "1111"), PathUtils.Combine(mPathPrefix, "2222"), settings, mLogger);
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, "2222", "file0.txt2")));
                //copy file over folder
                settings = new CopySettings();
                settings.Recursive = false;
                settings.Overwrite = true;
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "1111"));
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                mFilesystem.CreateDirectory(PathUtils.Combine(mPathPrefix, "3333"));
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), PathUtils.Combine(mPathPrefix, "3333"), settings, mLogger);
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix, "3333", "file0.txt2")));
                //copy folder
                if (mFilesystem.ExistsDirectory(mPathPrefix + "2")) {
                    mFilesystem.DeleteDirectory(mPathPrefix + "2");
                }
                mFilesystem.CreateDirectory(mPathPrefix + "2");
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                mFilesystem.Copy(mPathPrefix, mPathPrefix + "2", settings, mLogger);
                Assert.True(mFilesystem.ExistsDirectory(mPathPrefix + "2"));
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix + "2", "folder0", "file0.txt")));
                mFilesystem.DeleteDirectory(mPathPrefix + "2");
                Assert.False(mFilesystem.ExistsDirectory(mPathPrefix + "2"));
                //copy unexistent folder should raise exception
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                try {
                    mFilesystem.Copy(mPathPrefix + "2", mPathPrefix + "3", settings, mLogger);
                    Assert.True(false);
                } catch (Exception) {
                }
                //copy single file
                settings.Overwrite = false;
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger);
                string text = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix, "file0.txt2"));
                Assert.Equal(mFileContent, text);
                //copy single file over existing file, with overwrite should touch destination file
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                DateTime lastWriteTime1 = mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8).Modified;
                System.Threading.Thread.Sleep(2100);
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger);
                DateTime lastWriteTime2 = System.Convert.ToDateTime(mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file0.txt2")).Modified);
                Assert.NotEqual(lastWriteTime1, lastWriteTime2);
                //copy single file over existing file, without overwrite should not touch destination file
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = false;
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "file0.txt"), "hola que ase", System.Text.Encoding.UTF8);
                mFilesystem.SaveTextFile(PathUtils.Combine(mPathPrefix, "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                string lastWriteTime1str = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file0.txt2")).Modified.ToString("r");
                System.Threading.Thread.Sleep(2100);
                mFilesystem.Copy(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger);
                string lastWriteTime2str = mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file0.txt2")).Modified.ToString("r");
                Assert.Equal(lastWriteTime1str, lastWriteTime2str);

            }
        }

        [Fact()]
        public virtual async Task CopyAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                var settings = new CopySettings();
                //copy directory (should copy nothing)
                settings = new CopySettings();
                settings.Recursive = false;
                settings.Overwrite = true;
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "1111"), default);
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "2222"), default);
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "1111"), PathUtils.Combine(mPathPrefix, "2222"), settings, mLogger, default);
                Assert.False(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, "2222", "file0.txt2")));
                //copy directory
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "1111"), default);
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "2222"), default);
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "1111"), PathUtils.Combine(mPathPrefix, "2222"), settings, mLogger, default);
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, "2222", "file0.txt2")));
                //copy file over folder
                settings = new CopySettings();
                settings.Recursive = false;
                settings.Overwrite = true;
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "1111"), default);
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                await mFilesystem.CreateDirectoryAsync(PathUtils.Combine(mPathPrefix, "3333"), default);
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "1111", "file0.txt2"), PathUtils.Combine(mPathPrefix, "3333"), settings, mLogger, default);
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix, "3333", "file0.txt2")));
                //copy folder
                if (await mFilesystem.ExistsDirectoryAsync(mPathPrefix + "2")) {
                    await mFilesystem.DeleteDirectoryAsync(mPathPrefix + "2", default);
                }
                await mFilesystem.CreateDirectoryAsync(mPathPrefix + "2", default);
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                mFilesystem.Copy(mPathPrefix, mPathPrefix + "2", settings, mLogger);
                Assert.True(await mFilesystem.ExistsDirectoryAsync(mPathPrefix + "2"));
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix + "2", "folder0", "file0.txt")));
                await mFilesystem.DeleteDirectoryAsync(mPathPrefix + "2", default);
                Assert.False(await mFilesystem.ExistsDirectoryAsync(mPathPrefix + "2"));
                //copy unexistent folder should raise exception
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                try {
                    await mFilesystem.CopyAsync(mPathPrefix + "2", mPathPrefix + "3", settings, mLogger, default);
                    Assert.True(false);
                } catch (Exception) {
                }
                //copy single file
                settings.Overwrite = false;
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger, default);
                string text = await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix, "file0.txt2"));
                Assert.Equal(mFileContent, text);
                //copy single file over existing file, with overwrite should touch destination file
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                DateTime lastWriteTime1 = (await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8)).Modified;
                System.Threading.Thread.Sleep(2100);
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger, default);
                DateTime lastWriteTime2 = System.Convert.ToDateTime(mFilesystem.GetEntry(PathUtils.Combine(mPathPrefix, "file0.txt2"))!.Modified);
                Assert.NotEqual(lastWriteTime1, lastWriteTime2);
                //copy single file over existing file, without overwrite should not touch destination file
                settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = false;
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), "hola que ase", System.Text.Encoding.UTF8);
                await mFilesystem.SaveTextFileAsync(PathUtils.Combine(mPathPrefix, "file0.txt2"), "hola que ase", System.Text.Encoding.UTF8);
                string lastWriteTime1str = (await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "file0.txt2")))!.Modified.ToString("r");
                System.Threading.Thread.Sleep(2100);
                await mFilesystem.CopyAsync(PathUtils.Combine(mPathPrefix, "file0.txt"), PathUtils.Combine(mPathPrefix, "file0.txt2"), settings, mLogger, default);
                string lastWriteTime2str = (await mFilesystem.GetEntryAsync(PathUtils.Combine(mPathPrefix, "file0.txt2")))!.Modified.ToString("r");
                Assert.Equal(lastWriteTime1str, lastWriteTime2str);

            }
        }
        [Fact()]
        public virtual void Move() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                if (mFilesystem.ExistsDirectory(mPathPrefix + "2")) {
                    mFilesystem.DeleteDirectory(mPathPrefix + "2");
                }
                //move folder
                mFilesystem.Move(mPathPrefix, mPathPrefix + "2", new DProjects.Fs.MoveSettings(), mLogger);
                Assert.False(mFilesystem.ExistsDirectory(mPathPrefix));
                Assert.True(mFilesystem.ExistsDirectory(mPathPrefix + "2"));
                //test
                string text = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix + "2", "file0.txt"));
                Assert.Equal(mFileContent, text);
                //move file
                mFilesystem.Move(PathUtils.Combine(mPathPrefix + "2", "file0.txt"), PathUtils.Combine(mPathPrefix + "2", "file0.txt2"), new DProjects.Fs.MoveSettings(), mLogger);
                Assert.False(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix + "2", "file0.txt")));
                Assert.True(mFilesystem.ExistsFile(PathUtils.Combine(mPathPrefix + "2", "file0.txt2")));
                text = mFilesystem.LoadTextFile(PathUtils.Combine(mPathPrefix + "2", "file0.txt2"));
                Assert.Equal(mFileContent, text);
                //remove
                mFilesystem.DeleteDirectory(mPathPrefix + "2");
            }
        }
        [Fact()]
        public virtual async Task MoveAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                if (mFilesystem.ExistsDirectory(mPathPrefix + "2")) {
                    mFilesystem.DeleteDirectory(mPathPrefix + "2");
                }
                //move folder
                await mFilesystem.MoveAsync(mPathPrefix, mPathPrefix + "2", new DProjects.Fs.MoveSettings(), mLogger, default);
                Assert.False(await mFilesystem.ExistsDirectoryAsync(mPathPrefix));
                Assert.True(await mFilesystem.ExistsDirectoryAsync(mPathPrefix + "2"));
                //test
                string text = await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix + "2", "file0.txt"));
                Assert.Equal(mFileContent, text);
                //move file
                mFilesystem.Move(PathUtils.Combine(mPathPrefix + "2", "file0.txt"), PathUtils.Combine(mPathPrefix + "2", "file0.txt2"), new DProjects.Fs.MoveSettings(), mLogger);
                Assert.False(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix + "2", "file0.txt")));
                Assert.True(await mFilesystem.ExistsFileAsync(PathUtils.Combine(mPathPrefix + "2", "file0.txt2")));
                text = await mFilesystem.LoadTextFileAsync(PathUtils.Combine(mPathPrefix + "2", "file0.txt2"));
                Assert.Equal(mFileContent, text);
                //remove
                await mFilesystem.DeleteDirectoryAsync(mPathPrefix + "2", default);
            }
        }
        [Fact()]
        public virtual void Sync() {
        }
        [Fact()]
        public virtual Task SyncAsync() {
            return Task.CompletedTask;
        }

        [Fact()]
        public virtual void Metadata() {
            if (!mFilesystem.IsReadonly) {
                CreateFilesystemStructure();
                if (mFilesystem.Supports(mPathPrefix, Features.Metadata)) {
                    //simple test
                    var p = PathUtils.Combine(mPathPrefix, "hola.txt");
                    var metadata = new Dictionary<string, string>();
                    metadata.Add("var1", "1234");
                    metadata.Add("var2", "helloworld1");
                    metadata.Add("-var2", "helloworld2");
                    metadata.Add("-var3", "helloworld3");
                    
                    mFilesystem.SaveTextFile(p, "1234", System.Text.Encoding.UTF8);
                    mFilesystem.SetMetadata(p, metadata);
                    var metadata2 = mFilesystem.GetMetadata(p);

                    Assert.Equal(metadata.Count, metadata2.Count);
                    foreach(var key in metadata.Keys) {
                        Assert.Equal(metadata[key], metadata2[key]);
                    }

                    //case (key must be converted to lowercase)
                    metadata = new Dictionary<string, string>();
                    metadata.Add("VAR1", "1234");
                    mFilesystem.SetMetadata(p, metadata);
                    metadata2 = mFilesystem.GetMetadata(p);
                    foreach(var key in metadata2.Keys) {
                        Assert.Equal("var1", key, false);
                    }

                    //no repeats
                    metadata = new Dictionary<string, string>();
                    metadata.Add("VAR1", "1234");
                    metadata.Add("var1", "456");
                    mFilesystem.SetMetadata(p, metadata);
                    metadata2 = mFilesystem.GetMetadata(p);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //trim keys
                    metadata = new Dictionary<string, string>();
                    metadata.Add("  var1  ", "1234");
                    mFilesystem.SetMetadata(p, metadata);
                    metadata2 = mFilesystem.GetMetadata(p);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //metadata on folders
                    p = PathUtils.Combine(mPathPrefix, "myDir");
                    mFilesystem.CreateDirectory(p);
                    metadata = new Dictionary<string, string>();
                    metadata.Add("  var1  ", "1234");
                    mFilesystem.SetMetadata(p, metadata);
                    metadata2 = mFilesystem.GetMetadata(p);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //copy metadada
                    var p1 = PathUtils.Combine(mPathPrefix, "p1.txt");
                    var p2 = PathUtils.Combine(mPathPrefix, "p2.txt");
                    var metadata1 = new Dictionary<string, string>();
                    metadata1.Add("var1", "1234");
                    mFilesystem.SaveTextFile(p1, "1234", System.Text.Encoding.UTF8);
                    mFilesystem.SetMetadata(p1, metadata);
                    mFilesystem.Copy (p1,p2,new CopySettings(), mLogger);
                    var metadata1copied = mFilesystem.GetMetadata(p2);
                    Assert.Equal(metadata1.Count, metadata1copied.Count);
                    foreach (var key in metadata1.Keys) {
                        Assert.Equal(metadata1[key], metadata1copied[key]);
                    }

                    //copy metadada on directory
                    var d1 = PathUtils.Combine(mPathPrefix, "d1");
                    var d2 = PathUtils.Combine(mPathPrefix, "d2");
                    var metadatad1 = new Dictionary<string, string>();
                    metadatad1.Add("var1", "1234");
                    mFilesystem.CreateDirectory(d1);
                    mFilesystem.SetMetadata(d1, metadata);
                    mFilesystem.Copy(d1, d2, new CopySettings(), mLogger);
                    var metadatad1copied = mFilesystem.GetMetadata(d2);
                    Assert.Equal(metadatad1.Count, metadatad1copied.Count);
                    foreach (var key in metadatad1.Keys) {
                        Assert.Equal(metadatad1[key], metadatad1copied[key]);
                    }

                    //move metadada
                    var pp1 = PathUtils.Combine(mPathPrefix, "pp1.txt");
                    var pp2 = PathUtils.Combine(mPathPrefix, "pp2.txt");
                    var metadata11 = new Dictionary<string, string>();
                    metadata11.Add("var1", "1234");
                    mFilesystem.SaveTextFile(pp1, "1234", System.Text.Encoding.UTF8);
                    mFilesystem.SetMetadata(pp1, metadata);
                    mFilesystem.Move(pp1, pp2, new MoveSettings(), mLogger);
                    var metadata11moved = mFilesystem.GetMetadata(p2);
                    Assert.Equal(metadata11.Count, metadata11moved.Count);
                    foreach (var key in metadata11.Keys) {
                        Assert.Equal(metadata11[key], metadata11moved[key]);
                    }

                }
            }
        }
        [Fact()]
        public virtual async Task MetadataAsync() {
            if (!mFilesystem.IsReadonly) {
                await CreateFilesystemStructureAsync();
                if (await mFilesystem.SupportsAsync(mPathPrefix, Features.Metadata, default)) {
                    //simple test
                    var p = PathUtils.Combine(mPathPrefix, "hola.txt");
                    var metadata = new Dictionary<string, string>();
                    metadata.Add("var1", "1234");
                    metadata.Add("var2", "helloworld1");
                    metadata.Add("-var2", "helloworld2");
                    metadata.Add("-var3", "helloworld3");

                    await mFilesystem.SaveTextFileAsync(p, "1234", System.Text.Encoding.UTF8);
                    await mFilesystem.SetMetadataAsync(p, metadata, default);
                    var metadata2 = mFilesystem.GetMetadata(p);

                    Assert.Equal(metadata.Count, metadata2.Count);
                    foreach (var key in metadata.Keys) {
                        Assert.Equal(metadata[key], metadata2[key]);
                    }

                    //case (key must be converted to lowercase)
                    metadata = new Dictionary<string, string>();
                    metadata.Add("VAR1", "1234");
                    await mFilesystem.SetMetadataAsync(p, metadata, default);
                    metadata2 = await mFilesystem.GetMetadataAsync(p, default);
                    foreach (var key in metadata2.Keys) {
                        Assert.Equal("var1", key, false);
                    }

                    //no repeats
                    metadata = new Dictionary<string, string>();
                    metadata.Add("VAR1", "1234");
                    metadata.Add("var1", "456");
                    await mFilesystem.SetMetadataAsync(p, metadata, default);
                    metadata2 = await mFilesystem.GetMetadataAsync(p, default);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //trim keys
                    metadata = new Dictionary<string, string>();
                    metadata.Add("  var1  ", "1234");
                    await mFilesystem.SetMetadataAsync(p, metadata, default);
                    metadata2 = await mFilesystem.GetMetadataAsync(p, default);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //metadata on folders
                    p = PathUtils.Combine(mPathPrefix, "myDir");
                    await mFilesystem.CreateDirectoryAsync(p, default);
                    metadata = new Dictionary<string, string>();
                    metadata.Add("  var1  ", "1234");
                    await mFilesystem.SetMetadataAsync(p, metadata, default);
                    metadata2 = await mFilesystem.GetMetadataAsync(p, default);
                    Assert.Single(metadata2.Keys);
                    Assert.Equal("1234", metadata2["var1"]);

                    //copy metadada
                    var p1 = PathUtils.Combine(mPathPrefix, "p1.txt");
                    var p2 = PathUtils.Combine(mPathPrefix, "p2.txt");
                    var metadata1 = new Dictionary<string, string>();
                    metadata1.Add("var1", "1234");
                    await mFilesystem.SaveTextFileAsync(p1, "1234", System.Text.Encoding.UTF8);
                    await mFilesystem.SetMetadataAsync(p1, metadata, default);
                    mFilesystem.Copy(p1, p2, new CopySettings(), mLogger);
                    var metadata1copied = await mFilesystem.GetMetadataAsync(p2, default);
                    Assert.Equal(metadata1.Count, metadata1copied.Count);
                    foreach (var key in metadata1.Keys) {
                        Assert.Equal(metadata1[key], metadata1copied[key]);
                    }

                    //copy metadada on directory
                    var d1 = PathUtils.Combine(mPathPrefix, "d1");
                    var d2 = PathUtils.Combine(mPathPrefix, "d2");
                    var metadatad1 = new Dictionary<string, string>();
                    metadatad1.Add("var1", "1234");
                    await mFilesystem.CreateDirectoryAsync(d1, default);
                    await mFilesystem.SetMetadataAsync(d1, metadata, default);
                    await mFilesystem.CopyAsync(d1, d2, new CopySettings(), mLogger, default);
                    var metadatad1copied = await mFilesystem.GetMetadataAsync(d2, default);
                    Assert.Equal(metadatad1.Count, metadatad1copied.Count);
                    foreach (var key in metadatad1.Keys) {
                        Assert.Equal(metadatad1[key], metadatad1copied[key]);
                    }

                    //move metadada
                    var pp1 = PathUtils.Combine(mPathPrefix, "pp1.txt");
                    var pp2 = PathUtils.Combine(mPathPrefix, "pp2.txt");
                    var metadata11 = new Dictionary<string, string>();
                    metadata11.Add("var1", "1234");
                    await mFilesystem.SaveTextFileAsync(pp1, "1234", System.Text.Encoding.UTF8);
                    await mFilesystem.SetMetadataAsync(pp1, metadata, default);
                    await mFilesystem.MoveAsync(pp1, pp2, new MoveSettings(), mLogger, default);
                    var metadata11moved = await mFilesystem.GetMetadataAsync(p2, default);
                    Assert.Equal(metadata11.Count, metadata11moved.Count);
                    foreach (var key in metadata11.Keys) {
                        Assert.Equal(metadata11[key], metadata11moved[key]);
                    }

                }
            }
        }

    }


}