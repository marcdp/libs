using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Commands.Schema;
using DProjects.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Collections.Specialized;
using System.Text.Json;

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
        [Theory]
        [InlineData("-P", "name=John")]
        [InlineData("--param2", "name=John")]
        [InlineData("--param2=name=John", null)]
        [InlineData("-P=name=John", null)]
        public void Schema_ParsesStringDictionaryFlagSyntax(string option, string? value) {
            var command = new StringDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null);
            var args = value == null ? new[] { option } : new[] { option, value };

            var errors = Parse(schema, command, args);

            Assert.Empty(errors);
            Assert.Equal("John", command.Param2["name"]);
        }
        [Theory]
        [InlineData("-P")]
        [InlineData("--param2")]
        public void Schema_AccumulatesRepeatedStringDictionaryFlags(string option) {
            var command = new StringDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, [option, "name=John", option, "age=30", option, "city=Barcelona"]);

            Assert.Empty(errors);
            Assert.Equal("John", command.Param2["name"]);
            Assert.Equal("30", command.Param2["age"]);
            Assert.Equal("Barcelona", command.Param2["city"]);
        }
        [Fact]
        public void Schema_ParsesStringDictionaryFlagAliases() {
            var command = new AliasedStringDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(AliasedStringDictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["--parameters", "name=John"]);

            Assert.Empty(errors);
            Assert.Equal("John", command.Values["name"]);
        }
        [Fact]
        public void Schema_PreservesStringDictionaryEntrySemantics() {
            var command = new StringDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["-P", "name=", "-P", "token=first=second", "-P", "NAME=updated"]);

            Assert.Empty(errors);
            Assert.Equal(2, command.Param2.Count);
            Assert.Equal("updated", command.Param2["name"]);
            Assert.Equal("updated", command.Param2["NaMe"]);
            Assert.Equal("first=second", command.Param2["token"]);
        }
        [Theory]
        [InlineData("malformed")]
        [InlineData("=value")]
        public void Schema_ReportsInvalidStringDictionaryFlagEntries(string entry) {
            var command = new StringDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["-P", entry]);

            Assert.Contains(errors, error => error.Contains("flag --param2 invalid", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_HandlesOptionalAndRequiredStringDictionaryValues() {
            var optionalFlag = new StringDictionaryFlagCommand();
            var optionalArgument = new StringDictionaryArgumentCommand();
            var flagErrors = Parse(CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null),
                optionalFlag, []);
            var argumentErrors = Parse(CmdSchemaDefinition.Create(typeof(StringDictionaryArgumentCommand), "tests", "command", false, null),
                optionalArgument, []);
            var requiredFlagErrors = Parse(CmdSchemaDefinition.Create(typeof(RequiredStringDictionaryFlagCommand), "tests", "command", false, null),
                new RequiredStringDictionaryFlagCommand(), []);
            var requiredArgumentErrors = Parse(CmdSchemaDefinition.Create(typeof(RequiredStringDictionaryArgumentCommand), "tests", "command", false, null),
                new RequiredStringDictionaryArgumentCommand(), []);

            Assert.Empty(flagErrors);
            Assert.Empty(argumentErrors);
            Assert.Empty(optionalFlag.Param2);
            Assert.Empty(optionalArgument.Parameters);
            Assert.Contains(requiredFlagErrors, error => error.Contains("required", StringComparison.Ordinal));
            Assert.Contains(requiredArgumentErrors, error => error.Contains("required", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_CopiesStringDictionaryDefaultsForEachExecution() {
            var original = new StringDictionary { ["initial"] = "value" };
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryFlagCommand), "tests", "command", false, null);
            schema.Flags[0].Default = original;
            schema.Flags[0].Required = false;
            var first = new StringDictionaryFlagCommand();
            var second = new StringDictionaryFlagCommand();

            var firstErrors = Parse(schema, first, ["-P", "added=first"]);
            var secondErrors = Parse(schema, second, []);

            Assert.Empty(firstErrors);
            Assert.Empty(secondErrors);
            Assert.Equal(1, original.Count);
            Assert.Null(original["added"]);
            Assert.Equal("first", first.Param2["added"]);
            Assert.Equal("value", second.Param2["initial"]);
            Assert.NotSame(original, first.Param2);
            Assert.NotSame(original, second.Param2);
            Assert.NotSame(first.Param2, second.Param2);
        }
        [Fact]
        public void Schema_ParsesPositionalStringDictionaryEntries() {
            var command = new StringDictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryArgumentCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["name=John", "age=30", "city=Barcelona", "empty=", "token=first=second", "NAME=updated"]);

            Assert.Empty(errors);
            Assert.Equal(5, command.Parameters.Count);
            Assert.Equal("updated", command.Parameters["name"]);
            Assert.Equal("30", command.Parameters["age"]);
            Assert.Equal("Barcelona", command.Parameters["city"]);
            Assert.Equal("", command.Parameters["empty"]);
            Assert.Equal("first=second", command.Parameters["token"]);
        }
        [Fact]
        public void Schema_ConsumesEarlierPositionalArgumentsBeforeStringDictionaryEntries() {
            var command = new PrefixedStringDictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(PrefixedStringDictionaryArgumentCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["prefix", "name=John", "age=30"]);

            Assert.Empty(errors);
            Assert.Equal("prefix", command.Prefix);
            Assert.Equal("John", command.Parameters["name"]);
            Assert.Equal("30", command.Parameters["age"]);
        }
        [Theory]
        [InlineData("malformed")]
        [InlineData("=value")]
        public void Schema_ReportsInvalidPositionalStringDictionaryEntries(string entry) {
            var command = new StringDictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryArgumentCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, [entry]);

            Assert.Contains(errors, error => error.Contains("argument 'parameters' invalid", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_RequiresStringDictionaryToBeTheFinalPositionalArgument() {
            var exception = Assert.Throws<ArgumentException>(() =>
                CmdSchemaDefinition.Create(typeof(InvalidStringDictionaryOrderingCommand), "tests", "command", false, null));

            Assert.Contains("final positional argument", exception.Message, StringComparison.Ordinal);
        }
        [Fact]
        public void Schema_SerializesStringDictionaryTypeNamesAndDocumentsEntrySyntax() {
            var flag = new CmdSchemaFlag { Type = typeof(StringDictionary) };
            var json = JsonSerializer.Serialize(flag);
            var restored = JsonSerializer.Deserialize<CmdSchemaFlag>(json);
            var schema = CmdSchemaDefinition.Create(typeof(StringDictionaryHelpCommand), "tests", "command", false, null);

            Assert.Contains("\"Type\":\"StringDictionary\"", json, StringComparison.Ordinal);
            Assert.NotNull(restored);
            Assert.Equal(typeof(StringDictionary), restored.Type);
            Assert.Equal(typeof(StringDictionary), ConvertUtils.ToSimpleType("StringDictionary"));
            Assert.Equal("StringDictionary", ConvertUtils.FromSimpleType(typeof(StringDictionary)));
            Assert.Equal(2, DefinitionToHelp.GetHelpText(schema, false).Split("Entries use key=value syntax.").Length - 1);
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

    public sealed class StringDictionaryFlagCommand {

        // props
        [Flag('P', "parameters", "")] public StringDictionary Param2 { get; set; } = new();
    }

    public sealed class AliasedStringDictionaryFlagCommand {

        // props
        [Flag('P', "parameters", "", "parameters")] public StringDictionary Values { get; set; } = new();
    }

    public sealed class RequiredStringDictionaryFlagCommand {

        // props
        [Flag('P', "parameters", null)] public StringDictionary Parameters { get; set; } = new();
    }

    public sealed class StringDictionaryArgumentCommand {

        // props
        [Argument(0, "parameters", "")] public StringDictionary Parameters { get; set; } = new();
    }

    public sealed class RequiredStringDictionaryArgumentCommand {

        // props
        [Argument(0, "parameters", null)] public StringDictionary Parameters { get; set; } = new();
    }

    public sealed class PrefixedStringDictionaryArgumentCommand {

        // props
        [Argument(0, "prefix", null)] public string Prefix { get; set; } = "";
        [Argument(1, "parameters", "")] public StringDictionary Parameters { get; set; } = new();
    }

    public sealed class InvalidStringDictionaryOrderingCommand {

        // props
        [Argument(0, "parameters", "")] public StringDictionary Parameters { get; set; } = new();
        [Argument(1, "tail", "")] public string Tail { get; set; } = "";
    }

    public sealed class StringDictionaryHelpCommand {

        // props
        [Flag('P', "parameters", "")] public StringDictionary Options { get; set; } = new();
        [Argument(0, "parameters", "")] public StringDictionary Parameters { get; set; } = new();
    }

    public sealed class ExecutionRecorder {

        // props
        public string? Value { get; set; }
        public bool Enabled { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}