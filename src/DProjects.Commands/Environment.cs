using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Db;
using DProjects.Db.Writers;

namespace DProjects.Commands {

    public class Environment : IEnvironment {

        // inner classes
        public class Input : IInput {
            private Stream mStream;
            private StreamReader mStreamReader;
            public Input() {
                mStream = System.Console.OpenStandardInput();
                mStreamReader = new StreamReader(mStream, System.Console.InputEncoding, true, 1024, true);
            }
            public void Dispose() {
                mStream.Dispose();
            }
            public TextReader CreateTextReader() {
                return mStreamReader;
            }
            public async Task<string> ReadLineAsync(CancellationToken cancellationToken = default) {
                return await mStreamReader.ReadLineAsync();
            }
        }
        public class Output(Output.Mode mode) : IOutput {
            public enum Mode {
                Output,
                Error
            }
            public async Task WriteAsync(string text) {
                if (mode == Mode.Error) {
                    await System.Console.Error.WriteAsync(text);
                } else {
                    await System.Console.Out.WriteAsync(text);
                }
            }
            public async Task WriteLineAsync(string text) {
                if (mode == Mode.Error) {
                    await System.Console.Error.WriteLineAsync(text);
                } else {
                    await System.Console.Out.WriteLineAsync(text);
                }
            }
            public TextWriter CreateTextWriter() {
                if (mode == Mode.Error) {
                    return System.Console.Error;
                } else {
                    return System.Console.Out;
                }
            }
            public IDBWriter CreateDBWriter(string format) {
                if (format.IndexOf(":")==-1) format += ":";
                if (format.StartsWith("csv")) return new DBWriterCsvFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("jsonl")) return new DBWriterJsonLinesFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("json")) return new DBWriterJsonFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("plain")) return new DBWriterPlain(CreateTextWriter(), true);
                if (format.StartsWith("xml")) return new DBWriterXmlFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("html")) return new DBWriterHtmlFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("raw")) return new DBWriterRawFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("yaml")) return new DBWriterYamlFactory().Create(format, CreateTextWriter());
                if (format.StartsWith("yfm")) return new DBWriterYfmFactory().Create(format, CreateTextWriter());
                throw new System.Exception(format + " is not a valid format for output");
            }
            public bool IsTerminal {
                get {
                    if (mode == Mode.Error) {
                        return System.Console.IsErrorRedirected == false;
                    } else {
                        return System.Console.IsOutputRedirected == false;
                    }
                }
            }
        }

        // vars
        private CommandsManager mCommandsManager;

        // ctor
        public Environment(CommandsManager commandsManager) {
            In = new Input();
            Out = new Output(Output.Mode.Output);
            Err = new Output(Output.Mode.Error);
            mCommandsManager = commandsManager;
        }

        // props
        public IInput In { get; }
        public IOutput Out { get; }
        public IOutput Err { get; }

        // methods
        public void GetVariable(string name) {
            throw new System.NotImplementedException();
        }
        public void SetVariable(string name, string value) {
            throw new System.NotImplementedException();
        }
        public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken) {
            return await mCommandsManager.ExecuteAsync(args, cancellationToken);
        }
    }

}