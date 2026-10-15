using DProjects.Commands;
using DProjects.Commands.Attributes;
using DProjects.Commands.Schema;
using DProjects.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        [InlineData("--parameters", "name=John")]
        [InlineData("--parameters=name=John", null)]
        [InlineData("-P=name=John", null)]
        public void Schema_ParsesDictionaryFlagSyntax(string option, string? value) {
            var command = new DictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null);
            var args = value == null ? new[] { option } : new[] { option, value };

            var errors = Parse(schema, command, args);

            Assert.Empty(errors);
            Assert.Equal("John", command.Parameters["name"]);
        }
        [Theory]
        [InlineData("-P")]
        [InlineData("--parameters")]
        public void Schema_AccumulatesRepeatedDictionaryFlags(string option) {
            var command = new DictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, [option, "name=John", option, "age=30", option, "city=Barcelona"]);

            Assert.Empty(errors);
            Assert.Equal("John", command.Parameters["name"]);
            Assert.Equal("30", command.Parameters["age"]);
            Assert.Equal("Barcelona", command.Parameters["city"]);
        }
        [Fact]
        public void Schema_ParsesDictionaryFlagAliases() {
            var command = new AliasedDictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(AliasedDictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["--parameters", "name=John"]);

            Assert.Empty(errors);
            Assert.Equal("John", command.Values["name"]);
        }
        [Fact]
        public void Schema_PreservesDictionaryEntrySemantics() {
            var command = new DictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["-P", "name=", "-P", "token=first=second", "-P", "NAME=updated"]);

            Assert.Empty(errors);
            Assert.Equal(2, command.Parameters.Count);
            Assert.Equal("updated", command.Parameters["name"]);
            Assert.Equal("updated", command.Parameters["NaMe"]);
            Assert.Equal("first=second", command.Parameters["token"]);
            Assert.Contains(command.Parameters.Keys, key => key == "name");
            Assert.DoesNotContain(command.Parameters.Keys, key => key == "NAME");
        }
        [Theory]
        [InlineData("malformed")]
        [InlineData("=value")]
        public void Schema_ReportsInvalidDictionaryFlagEntries(string entry) {
            var command = new DictionaryFlagCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["-P", entry]);

            Assert.Contains(errors, error => error.Contains("flag --parameters invalid", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_HandlesOptionalAndRequiredDictionaryValues() {
            var optionalFlag = new DictionaryFlagCommand();
            var optionalArgument = new DictionaryArgumentCommand();
            var flagErrors = Parse(CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null),
                optionalFlag, []);
            var argumentErrors = Parse(CmdSchemaDefinition.Create(typeof(DictionaryArgumentCommand), "tests", "command", false, null),
                optionalArgument, []);
            var requiredFlagErrors = Parse(CmdSchemaDefinition.Create(typeof(RequiredDictionaryFlagCommand), "tests", "command", false, null),
                new RequiredDictionaryFlagCommand(), []);
            var requiredArgumentErrors = Parse(CmdSchemaDefinition.Create(typeof(RequiredDictionaryArgumentCommand), "tests", "command", false, null),
                new RequiredDictionaryArgumentCommand(), []);

            Assert.Empty(flagErrors);
            Assert.Empty(argumentErrors);
            Assert.Empty(optionalFlag.Parameters);
            Assert.Empty(optionalArgument.Parameters);
            Assert.Contains(requiredFlagErrors, error => error.Contains("required", StringComparison.Ordinal));
            Assert.Contains(requiredArgumentErrors, error => error.Contains("required", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_CopiesDictionaryDefaultsForEachExecution() {
            var original = new Dictionary<string, string> { ["Initial"] = "value" };
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryFlagCommand), "tests", "command", false, null);
            schema.Flags[0].Default = original;
            schema.Flags[0].Required = false;
            var first = new DictionaryFlagCommand();
            var second = new DictionaryFlagCommand();

            var firstErrors = Parse(schema, first, ["-P", "added=first"]);
            var secondErrors = Parse(schema, second, []);

            Assert.Empty(firstErrors);
            Assert.Empty(secondErrors);
            Assert.Single(original);
            Assert.False(original.ContainsKey("added"));
            Assert.Equal("first", first.Parameters["added"]);
            Assert.Equal("value", second.Parameters["initial"]);
            Assert.Contains(second.Parameters.Keys, key => key == "Initial");
            Assert.NotSame(original, first.Parameters);
            Assert.NotSame(original, second.Parameters);
            Assert.NotSame(first.Parameters, second.Parameters);
        }
        [Fact]
        public void Schema_ParsesPositionalDictionaryEntries() {
            var command = new DictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryArgumentCommand), "tests", "command", false, null);

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
        public void Schema_ConsumesEarlierPositionalArgumentsBeforeDictionaryEntries() {
            var command = new PrefixedDictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(PrefixedDictionaryArgumentCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, ["prefix", "name=John", "age=30"]);

            Assert.Empty(errors);
            Assert.Equal("prefix", command.Prefix);
            Assert.Equal("John", command.Parameters["name"]);
            Assert.Equal("30", command.Parameters["age"]);
        }
        [Theory]
        [InlineData("malformed")]
        [InlineData("=value")]
        public void Schema_ReportsInvalidPositionalDictionaryEntries(string entry) {
            var command = new DictionaryArgumentCommand();
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryArgumentCommand), "tests", "command", false, null);

            var errors = Parse(schema, command, [entry]);

            Assert.Contains(errors, error => error.Contains("argument 'parameters' invalid", StringComparison.Ordinal));
        }
        [Fact]
        public void Schema_RequiresDictionaryToBeTheFinalPositionalArgument() {
            var exception = Assert.Throws<ArgumentException>(() =>
                CmdSchemaDefinition.Create(typeof(InvalidDictionaryOrderingCommand), "tests", "command", false, null));

            Assert.Contains("final positional argument", exception.Message, StringComparison.Ordinal);
        }
        [Fact]
        public void Schema_SerializesDictionaryTypeNamesAndDocumentsEntrySyntax() {
            var flagJson = JsonSerializer.Serialize(new CmdSchemaFlag { Type = typeof(Dictionary<string, string>) });
            var argumentJson = JsonSerializer.Serialize(new CmdSchemaArgument { Type = typeof(Dictionary<string, string>) });
            var restoredFlag = JsonSerializer.Deserialize<CmdSchemaFlag>(flagJson);
            var restoredArgument = JsonSerializer.Deserialize<CmdSchemaArgument>(argumentJson);
            var schema = CmdSchemaDefinition.Create(typeof(DictionaryHelpCommand), "tests", "command", false, null);

            Assert.Equal("Dictionary<string,string>", JsonDocument.Parse(flagJson).RootElement.GetProperty("Type").GetString());
            Assert.Equal("Dictionary<string,string>", JsonDocument.Parse(argumentJson).RootElement.GetProperty("Type").GetString());
            Assert.NotNull(restoredFlag);
            Assert.NotNull(restoredArgument);
            Assert.Equal(typeof(Dictionary<string, string>), restoredFlag.Type);
            Assert.Equal(typeof(Dictionary<string, string>), restoredArgument.Type);
            Assert.Equal(typeof(Dictionary<string, string>), ConvertUtils.ToSimpleType("Dictionary<string,string>"));
            Assert.Equal("Dictionary<string,string>", ConvertUtils.FromSimpleType(typeof(Dictionary<string, string>)));
            Assert.Equal(2, DefinitionToHelp.GetHelpText(schema, false).Split("Entries use key=value syntax.").Length - 1);
        }
        [Fact]
        public void Schema_PreservesDictionaryResolutionWithRemainingAndRawArguments() {
            var remainingCommand = new DictionaryWithRemainingCommand();
            var rawCommand = new DictionaryWithRawCommand();

            var remainingErrors = Parse(CmdSchemaDefinition.Create(typeof(DictionaryWithRemainingCommand), "tests", "command", false, null),
                remainingCommand, ["name=John", "age=30"]);
            var rawErrors = Parse(CmdSchemaDefinition.Create(typeof(DictionaryWithRawCommand), "tests", "command", false, null),
                rawCommand, ["-P", "name=John", "literal"]);

            Assert.Empty(remainingErrors);
            Assert.Equal("John", remainingCommand.Parameters["name"]);
            Assert.Equal("30", remainingCommand.Parameters["age"]);
            Assert.Empty(remainingCommand.Remaining);
            Assert.Empty(rawErrors);
            Assert.Empty(rawCommand.Parameters);
            Assert.Equal(["-P", "name=John", "literal"], rawCommand.Raw);
        }
        [Fact]
        public async Task Manager_ExecutesCommandWithInitDictionaryProperty() {
            var recorder = new ExecutionRecorder();
            await using var provider = CreateProvider(recorder);
            var manager = provider.GetRequiredService<CommandsManager>();

            var result = await manager.ExecuteAsync(["dictionary", "-P", "Name=John", "--parameters=age=30", "-P=NAME=Jane"],
                TestContext.Current.CancellationToken);

            Assert.Equal(43, result);
            var parameters = Assert.IsType<Dictionary<string, string>>(recorder.Parameters);
            Assert.Equal(2, parameters.Count);
            Assert.Equal("Jane", parameters["name"]);
            Assert.Equal("30", parameters["AGE"]);
            Assert.Contains(parameters.Keys, key => key == "Name");
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

    public sealed class DictionaryFlagCommand {

        // props
        [Flag('P', "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class AliasedDictionaryFlagCommand {

        // props
        [Flag('P', "parameters", "", "parameters")] public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RequiredDictionaryFlagCommand {

        // props
        [Flag('P', "parameters", null)] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class DictionaryArgumentCommand {

        // props
        [Argument(0, "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class RequiredDictionaryArgumentCommand {

        // props
        [Argument(0, "parameters", null)] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class PrefixedDictionaryArgumentCommand {

        // props
        [Argument(0, "prefix", null)] public string Prefix { get; set; } = "";
        [Argument(1, "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class InvalidDictionaryOrderingCommand {

        // props
        [Argument(0, "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [Argument(1, "tail", "")] public string Tail { get; set; } = "";
    }

    public sealed class DictionaryHelpCommand {

        // props
        [Flag('P', "parameters", "")] public Dictionary<string, string> Options { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [Argument(0, "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class DictionaryWithRemainingCommand {

        // props
        [Argument(0, "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [Remaining("remaining", "", false)] public string[] Remaining { get; set; } = [];
    }

    public sealed class DictionaryWithRawCommand {

        // props
        [Flag('P', "parameters", "")] public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [Raw("raw", "", false)] public string[] Raw { get; set; } = [];
    }

    [Name("dictionary")]
    public sealed class DictionaryCommand(ExecutionRecorder recorder) : ICommand {

        // props
        [Flag('P', "parameters", "", "parameters")]
        public Dictionary<string, string> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        // methods
        public Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            recorder.Parameters = Parameters;
            return Task.FromResult(43);
        }
    }

    public sealed class ExecutionRecorder {

        // props
        public string? Value { get; set; }
        public bool Enabled { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public Dictionary<string, string>? Parameters { get; set; }
    }
}
