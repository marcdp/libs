using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Factories;
using DProjects.Secrets;

namespace DProjects.Secrets.Tests {

    public class SecretManagerTests {

        // methods
        [Fact]
        public async Task Mem_ImplementsSealingPasswordRotationAndCrudContract() {
            var manager = new SecretManagerMem("old-password");

            await AssertManagerContract(manager, "old-password");
            await manager.Seal("new-password", TestContext.Current.CancellationToken);
            Assert.False(await manager.Unseal("old-password", TestContext.Current.CancellationToken));
            Assert.True(await manager.IsSealed(TestContext.Current.CancellationToken));
            Assert.True(await manager.Unseal("new-password", TestContext.Current.CancellationToken));
            Assert.Equal("updated", (await manager.GetAsync("app/token", TestContext.Current.CancellationToken))!.GetValue());
        }
        [Fact]
        public async Task MutableManagers_RejectEveryDataOperationWhileSealedWithoutLeakingSensitiveValues() {
            var managers = new ISecretManager[] {
                new SecretManagerMem("synthetic-password"),
                CreateJsonManager(new FilesystemMem(false, false), true)
            };

            foreach (var manager in managers) {
                var secret = new Secret("private-name", "", "highly-sensitive-value");
                var exceptions = new[] {
                    await Record.ExceptionAsync(() => manager.ListAsync(null, TestContext.Current.CancellationToken)),
                    await Record.ExceptionAsync(() => manager.GetAsync(secret.Name, TestContext.Current.CancellationToken)),
                    await Record.ExceptionAsync(() => manager.SetAsync(secret, TestContext.Current.CancellationToken)),
                    await Record.ExceptionAsync(() => manager.DelAsync(secret.Name, TestContext.Current.CancellationToken))
                };

                Assert.All(exceptions, exception => {
                    Assert.NotNull(exception);
                    Assert.DoesNotContain("highly-sensitive-value", exception.Message, StringComparison.Ordinal);
                    Assert.DoesNotContain("synthetic-password", exception.Message, StringComparison.Ordinal);
                });
            }
        }
        [Fact]
        public async Task Json_PersistsCrudAcrossManagerInstancesAndPasswordRotation() {
            using var filesystem = new FilesystemMem(false, false);
            var first = CreateJsonManager(filesystem, true);
            Assert.True(await first.Unseal("old-password", TestContext.Current.CancellationToken));
            await first.SetAsync(CreateSecret("app/token", "first"), TestContext.Current.CancellationToken);
            await first.SetAsync(CreateSecret("app/other", "other"), TestContext.Current.CancellationToken);
            await first.SetAsync(CreateSecret("app/token", "updated"), TestContext.Current.CancellationToken);
            await first.DelAsync("app/other", TestContext.Current.CancellationToken);
            await first.Seal("new-password", TestContext.Current.CancellationToken);

            var reopened = CreateJsonManager(filesystem, false);
            Assert.False(await reopened.Unseal("old-password", TestContext.Current.CancellationToken));
            Assert.True(await reopened.IsSealed(TestContext.Current.CancellationToken));
            Assert.True(await reopened.Unseal("new-password", TestContext.Current.CancellationToken));
            var persisted = await reopened.GetAsync("app/token", TestContext.Current.CancellationToken);

            Assert.NotNull(persisted);
            Assert.Equal("updated", persisted.GetValue());
            Assert.Null(await reopened.GetAsync("app/other", TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Json_EmptyStoreReopensAsAnEmptyUnsealedManager() {
            using var filesystem = new FilesystemMem(false, false);
            var first = CreateJsonManager(filesystem, true);
            Assert.True(await first.Unseal("password", TestContext.Current.CancellationToken));
            await first.Seal(TestContext.Current.CancellationToken);

            var reopened = CreateJsonManager(filesystem, false);
            Assert.True(await reopened.Unseal("password", TestContext.Current.CancellationToken));
            Assert.Empty(await reopened.ListAsync(null, TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Json_CorruptedPersistenceFailsWithoutUnsealingOrLeakingPayload() {
            using var filesystem = new FilesystemMem(false, false);
            filesystem.SaveTextFile("/secrets.aes", "synthetic-corrupt-payload", System.Text.Encoding.UTF8);
            var manager = CreateJsonManager(filesystem, false);

            var exception = await Record.ExceptionAsync(() => manager.Unseal("password", TestContext.Current.CancellationToken));

            Assert.NotNull(exception);
            Assert.True(await manager.IsSealed(TestContext.Current.CancellationToken));
            Assert.DoesNotContain("synthetic-corrupt-payload", exception.Message, StringComparison.Ordinal);
        }
        [Fact]
#pragma warning disable xUnit1051 // deliberately verifies pre-canceled filesystem operations
        public async Task Json_IoOperationsObserveCancellation() {
            using var filesystem = new FilesystemMem(false, false);
            var manager = CreateJsonManager(filesystem, true);
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.Unseal("password", source.Token));
        }
#pragma warning restore xUnit1051
        [Fact]
        public async Task NullManager_HasExplicitNoOpSemantics() {
            var manager = new SecretManagerNull();

            Assert.True(await manager.IsSealed(TestContext.Current.CancellationToken));
            Assert.False(await manager.Unseal("password", TestContext.Current.CancellationToken));
            Assert.Empty(await manager.ListAsync(null, TestContext.Current.CancellationToken));
            Assert.Null(await manager.GetAsync("missing", TestContext.Current.CancellationToken));
            await manager.SetAsync(CreateSecret("ignored", "value"), TestContext.Current.CancellationToken);
            Assert.False(await manager.DelAsync("ignored", TestContext.Current.CancellationToken));
            await manager.Seal(TestContext.Current.CancellationToken);
            await manager.Seal("replacement", TestContext.Current.CancellationToken);
        }
        [Fact]
        public async Task MemAndNullFactories_CreateManagersWithConfiguredSemantics() {
            var mem = new SecretManagerMemFactory().Create("mem:?password=synthetic");
            var none = new SecretManagerNullFactory().Create("null:");

            Assert.False(await mem.Unseal("wrong", TestContext.Current.CancellationToken));
            Assert.True(await mem.Unseal("synthetic", TestContext.Current.CancellationToken));
            Assert.IsType<SecretManagerNull>(none);
        }
        [Fact]
        public async Task JsonFactory_ForwardsInnerFilesystemUrlPathAndOuterInitOption() {
            using var filesystem = new FilesystemMem(false, false);
            var filesystemFactory = new RecordingFilesystemFactory(filesystem);
            var manager = new SecretManagerJsonFactory(filesystemFactory).Create("json:mem:!/vault.aes?init=true");

            Assert.Equal("mem:", filesystemFactory.Source);
            Assert.True(await manager.Unseal("synthetic", TestContext.Current.CancellationToken));
            Assert.True(filesystem.ExistsFile("/vault.aes"));
        }

        // methods (private)
        private static async Task AssertManagerContract(ISecretManager manager, string password) {
            Assert.True(await manager.IsSealed(TestContext.Current.CancellationToken));
            Assert.False(await manager.Unseal("wrong-password", TestContext.Current.CancellationToken));
            Assert.True(await manager.Unseal(password, TestContext.Current.CancellationToken));
            Assert.False(await manager.IsSealed(TestContext.Current.CancellationToken));
            Assert.Empty(await manager.ListAsync(null, TestContext.Current.CancellationToken));
            await manager.SetAsync(CreateSecret("app/token", "first"), TestContext.Current.CancellationToken);
            await manager.SetAsync(CreateSecret("app/other", "other"), TestContext.Current.CancellationToken);
            await manager.SetAsync(CreateSecret("app/token", "updated"), TestContext.Current.CancellationToken);
            Assert.Equal("updated", (await manager.GetAsync("app/token", TestContext.Current.CancellationToken))!.GetValue());
            Assert.Equal(2, (await manager.ListAsync("app/*", TestContext.Current.CancellationToken)).Length);
            Assert.Empty(await manager.ListAsync("missing/*", TestContext.Current.CancellationToken));
            Assert.True(await manager.DelAsync("app/other", TestContext.Current.CancellationToken));
            Assert.False(await manager.DelAsync("app/other", TestContext.Current.CancellationToken));
            Assert.Null(await manager.GetAsync("app/other", TestContext.Current.CancellationToken));
            await manager.Seal(TestContext.Current.CancellationToken);
            Assert.True(await manager.IsSealed(TestContext.Current.CancellationToken));
        }
        private static SecretManagerJson CreateJsonManager(IFilesystem filesystem, bool init) {
            return new SecretManagerJson(filesystem, "/secrets.aes", init);
        }
        private static Secret CreateSecret(string name, string value) {
            return new Secret(name, "synthetic test secret", value);
        }

        private sealed class RecordingFilesystemFactory(IFilesystem filesystem) : IFactoryByUrl<IFilesystem> {

            // props
            public string Source { get; private set; } = "";

            // methods
            public IFilesystem Create(string src) {
                Source = src;
                return filesystem;
            }
        }
    }
}
