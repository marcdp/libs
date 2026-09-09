using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Commands.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Commands.Tests {

    public class CommandsTests {

        // methods
        [Fact]
        public void Schema_ParsesPositionFlagsRepeatedValuesAndRemainingArguments() {
            var command = new ParseCommand();
            var schema = CmdSchemaDefinition.Create(typeof(ParseCommand), "tests", "command", false, null);
            var errors = new List<string>();

            var valid = schema.InitializeObjectProperties(command,
                ["input value", "--verbose", "--tag=one", "-t", "two words", "tail-1", "tail-2"], null, null, null, errors, (_, _) => new object());

            Assert.True(valid);
            Assert.Empty(errors);
            Assert.Equal("input value", command.Input);
            Assert.True(command.Verbose);
            Assert.Equal(["one", "two words"], command.Tags);
            Assert.Equal(["tail-1", "tail-2"], command.Remaining);
        }
        [Fact]
        public void Schema_ReportsMissingRequiredValuesUnknownOptionsAndInvalidConversions() {
            var schema = CmdSchemaDefinition.Create(typeof(StrictCommand), "tests", "command", false, null);

            var missingErrors = Parse(schema, new StrictCommand(), []);
            var unknownErrors = Parse(schema, new StrictCommand(), ["42", "--unknown"]);
            var invalidErrors = Parse(schema, new StrictCommand(), ["not-an-int"]);

            Assert.Contains(missingErrors, error => error.Contains("required", StringComparison.Ordinal));
            Assert.Contains(unknownErrors, error => error.Contains("invalid", StringComparison.Ordinal));
            Assert.Contains(invalidErrors, error => error.Contains("invalid", StringComparison.Ordinal));
        }
        [Fact]
        public async Task Manager_SelectsCommandInjectsArgumentsAndReturnsExecutionResult() {
            var recorder = new ExecutionRecorder();
            await using var provider = CreateProvider(recorder);
            var manager = provider.GetRequiredService<CommandsManager>();

            var result = await manager.ExecuteAsync(["record", "payload", "--enabled"], TestContext.Current.CancellationToken);

            Assert.Equal(37, result);
            Assert.Equal("payload", recorder.Value);
            Assert.True(recorder.Enabled);
            Assert.Equal(TestContext.Current.CancellationToken, recorder.CancellationToken);
        }
        [Fact]
        public async Task Manager_PropagatesCommandExceptionsAndCancellation() {
            await using var provider = CreateProvider(new ExecutionRecorder());
            var manager = provider.GetRequiredService<CommandsManager>();

            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ExecuteAsync(["failure"], TestContext.Current.CancellationToken));
            Assert.Equal("synthetic command failure", failure.Message);
            using var source = new CancellationTokenSource();
            source.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.ExecuteAsync(["cancel"], source.Token));
        }
        [Fact]
        public async Task Manager_UnknownCommandReturnsThePublishedNotFoundExitCode() {
            await using var provider = CreateProvider(new ExecutionRecorder());
            var manager = provider.GetRequiredService<CommandsManager>();
            var original = Console.Out;
            using var output = new StringWriter();
            try {
                Console.SetOut(output);

                var result = await manager.ExecuteAsync(["does-not-exist"], TestContext.Current.CancellationToken);

                Assert.Equal(Errors.ERROR_COMMAND_NOT_FOUND, result);
                Assert.Contains("commands:", output.ToString(), StringComparison.Ordinal);
            } finally {
                Console.SetOut(original);
            }
        }
        [Fact]
        public void Configuration_DuplicateCommandDiscoveryIsRejected() {
            var configuration = new Configuration(new ServiceCollection(), "tests");
            configuration.AddCommandsFromAssembly(typeof(CommandsTests).Assembly);

            Assert.Throws<ArgumentException>(() => configuration.AddCommandsFromAssembly(typeof(CommandsTests).Assembly));
        }
        [Fact]
        public void Configuration_AddGlobalFlagAddsParseableMetadataToEveryRegisteredCommand() {
            var configuration = new Configuration(new ServiceCollection(), "tests");
            configuration.AddCommandsFromAssembly(typeof(CommandsTests).Assembly);

            configuration.AddGlobalFlag('g', "global-value", "global value", "fallback");

            Assert.All(configuration.Commands.Values, command => {
                var flag = Assert.Single(command.Flags, candidate => candidate.Name == "global-value");
                Assert.Equal('g', flag.Char);
                Assert.Equal("fallback", flag.Default);
                Assert.False(flag.Required);
            });
        }
        [Fact]
        public void EnvironmentVariableStubs_AreExplicitlyUnsupportedWithoutChangingThePublicApi() {
            var environment = new DProjects.Commands.Environment(null!);

            Assert.Throws<NotSupportedException>(() => environment.GetVariable("DPROJECTS_TEST_SYNTHETIC"));
            Assert.Throws<NotSupportedException>(() => environment.SetVariable("DPROJECTS_TEST_SYNTHETIC", "value"));
        }
        [Fact]
        public void OutputFactory_SelectsKnownFormatsAndRejectsUnknownFormats() {
            var output = new DProjects.Commands.Environment.Output(DProjects.Commands.Environment.Output.Mode.Output);

            Assert.IsAssignableFrom<DProjects.Db.IDBWriter>(output.CreateDBWriter("json"));
            Assert.Throws<Exception>(() => output.CreateDBWriter("unknown"));
        }

        // methods (private)
        private static List<string> Parse(CmdSchemaDefinition schema, object command, string[] args) {
            var errors = new List<string>();
            schema.InitializeObjectProperties(command, args, null, null, null, errors, (_, _) => new object());
            return errors;
        }
        private static ServiceProvider CreateProvider(ExecutionRecorder recorder) {
            var services = new ServiceCollection();
            services.AddSingleton(recorder);
            services.AddSingleton<ILogger<CommandsManager>>(NullLogger<CommandsManager>.Instance);
            services.AddCommandsManager(configuration => configuration.AddCommandsFromAssembly(typeof(CommandsTests).Assembly));
            return services.BuildServiceProvider();
        }
    }

    [Name("record")]
    public sealed class RecordCommand(ExecutionRecorder recorder) : ICommand {

        // props
        [Argument(0, "value", null)] public string Value { get; set; } = "";
        [Flag('e', "enabled", false)] public bool Enabled { get; set; }

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            recorder.Value = Value;
            recorder.Enabled = Enabled;
            recorder.CancellationToken = cancellationToken;
            return Task.FromResult(37);
        }
    }

    [Name("failure")]
    public sealed class FailureCommand : ICommand {

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            throw new InvalidOperationException("synthetic command failure");
        }
    }

    [Name("cancel")]
    public sealed class CancelCommand : ICommand {

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            return Task.FromCanceled<int>(cancellationToken);
        }
    }

    public sealed class ParseCommand : ICommand {

        // props
        [Argument(0, "input", null)] public string Input { get; set; } = "";
        [Flag('v', "verbose", false)] public bool Verbose { get; set; }
        [Flag('t', "tags", new string[0], "tag")] public string[] Tags { get; set; } = [];
        [Remaining("remaining", "", false)] public string[] Remaining { get; set; } = [];

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    public sealed class StrictCommand : ICommand {

        // props
        [Argument(0, "number", null)] public int Number { get; set; }

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    public sealed class ExecutionRecorder {

        // props
        public string? Value { get; set; }
        public bool Enabled { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
