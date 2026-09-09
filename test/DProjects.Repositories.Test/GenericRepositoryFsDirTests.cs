using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Repositories;

namespace DProjects.Repositories.Tests {

    public class GenericRepositoryFsDirTests {

        // methods
        [Theory]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Json)]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Yaml)]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Yfm)]
        public async Task AddGetAndSave_RoundTripDomainValuesForEveryFormat(GenericRepositoryFsDir<TestEntity, string>.Formats format) {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, format);
            var entity = CreateEntity("alpha", "first");

            await repository.AddAsync(entity, TestContext.Current.CancellationToken);
            var added = await repository.GetAsync(entity.Id, TestContext.Current.CancellationToken);
            entity.Name = "updated";
            entity.Count = 42;
            entity.Optional = null;
            entity.Tags = ["updated", "round-trip"];
            entity.Content = "updated body\nwith a second line";
            await repository.SaveAsync(entity, TestContext.Current.CancellationToken);
            var saved = await repository.GetAsync(entity.Id, TestContext.Current.CancellationToken);

            AssertEquivalent(CreateEntity("alpha", "first"), added!);
            AssertEquivalent(entity, saved!);
        }
        [Theory]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Json)]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Yaml)]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Yfm)]
        public async Task MissingRemoveAndList_HaveStableCrudSemantics(GenericRepositoryFsDir<TestEntity, string>.Formats format) {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, format);
            await repository.AddAsync(CreateEntity("alpha", "one"), TestContext.Current.CancellationToken);
            await repository.AddAsync(CreateEntity("beta", "two"), TestContext.Current.CancellationToken);
            await repository.AddAsync(CreateEntity("alpine", "three"), TestContext.Current.CancellationToken);

            Assert.Null(await repository.GetAsync("missing", TestContext.Current.CancellationToken));
            Assert.Equal(new[] { "alpha", "alpine", "beta" }, (await Collect(repository.ListAsync("*", TestContext.Current.CancellationToken))).Select(x => x.Id).Order());
            Assert.Equal(new[] { "alpha", "alpine" }, (await Collect(repository.ListAsync("alp*", TestContext.Current.CancellationToken))).Select(x => x.Id).Order());
            Assert.Empty(await Collect(repository.ListAsync("missing*", TestContext.Current.CancellationToken)));
            await repository.RemoveAsync("beta", TestContext.Current.CancellationToken);
            await repository.RemoveAsync("beta", TestContext.Current.CancellationToken);
            Assert.Null(await repository.GetAsync("beta", TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Yfm_PreservesFrontMatterFieldsAndContentBody() {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, GenericRepositoryFsDir<TestEntity, string>.Formats.Yfm);
            var entity = CreateEntity("article", "front matter");
            entity.Content = "# Heading\n\nBody text.";

            await repository.AddAsync(entity, TestContext.Current.CancellationToken);
            var result = await repository.GetAsync("article", TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.Equal("front matter", result.Name);
            Assert.Equal("# Heading\n\nBody text.", NormalizeLineEndings(result.Content));
        }
        [Theory]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Json, "{ definitely not json")]
        [InlineData(GenericRepositoryFsDir<TestEntity, string>.Formats.Yaml, "name: [unterminated")]
        public async Task MalformedPersistedData_IsReported(GenericRepositoryFsDir<TestEntity, string>.Formats format, string malformed) {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, format);
            filesystem.SaveTextFile($"/repository/broken.{format.ToString().ToLowerInvariant()}", malformed, System.Text.Encoding.UTF8);

            await Assert.ThrowsAnyAsync<Exception>(() => repository.GetAsync("broken", TestContext.Current.CancellationToken));
        }
        [Fact]
        public void InvalidFormat_IsRejectedAtConstruction() {
            using var filesystem = new FilesystemMem(false, false);

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRepository(filesystem, (GenericRepositoryFsDir<TestEntity, string>.Formats)999));
        }
        [Theory]
        [InlineData("../outside")]
        [InlineData("folder/child")]
        [InlineData("folder\\child")]
        public async Task UnsafeIds_AreRejectedAndCannotEscapeRepositoryRoot(string id) {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, GenericRepositoryFsDir<TestEntity, string>.Formats.Json);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.AddAsync(CreateEntity(id, "unsafe"), TestContext.Current.CancellationToken));

            Assert.Empty(filesystem.GetEntries("/", GetModes.Files));
        }
        [Fact]
#pragma warning disable xUnit1051 // deliberately verifies pre-canceled filesystem operations
        public async Task FilesystemOperations_ObserveCancellationIncludingListEnumeration() {
            using var filesystem = new FilesystemMem(false, false);
            var repository = CreateRepository(filesystem, GenericRepositoryFsDir<TestEntity, string>.Formats.Json);
            await repository.AddAsync(CreateEntity("existing", "value"), TestContext.Current.CancellationToken);
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetAsync("existing", source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.AddAsync(CreateEntity("add", "value"), source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(CreateEntity("existing", "changed"), source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync("existing", source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await Collect(repository.ListAsync("*", source.Token)));
        }
#pragma warning restore xUnit1051

        // methods (private)
        private static GenericRepositoryFsDir<TestEntity, string> CreateRepository(IFilesystem filesystem, GenericRepositoryFsDir<TestEntity, string>.Formats format) {
            return new GenericRepositoryFsDir<TestEntity, string>(filesystem, "/repository", format);
        }
        private static TestEntity CreateEntity(string id, string name) {
            return new TestEntity { Id = id, Name = name, Count = 7, Optional = "optional", Tags = ["one", "two"], Content = "body" };
        }
        private static void AssertEquivalent(TestEntity expected, TestEntity actual) {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected.Optional, actual.Optional);
            Assert.Equal(expected.Tags, actual.Tags);
            Assert.Equal(NormalizeLineEndings(expected.Content), NormalizeLineEndings(actual.Content));
        }
        private static async Task<List<TestEntity>> Collect(IAsyncEnumerable<TestEntity> source) {
            var result = new List<TestEntity>();
            await foreach (var item in source) result.Add(item);
            return result;
        }
        private static string NormalizeLineEndings(string value) {
            return value.Replace("\r\n", "\n");
        }

        public class TestEntity : IGenericRepositoryElement<string> {

            // props
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public int Count { get; set; }
            public string? Optional { get; set; }
            public string[] Tags { get; set; } = [];
            public string Content { get; set; } = "";
        }
    }
}
