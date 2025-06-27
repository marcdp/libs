using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DProjects.Commands {

    public class Environment : IEnvironment {

        // inner classes
        public class Input : IInput {
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
        }

        // ctor
        public Environment() {
            In = new Input();
            Out = new Output(Output.Mode.Output);
            Err = new Output(Output.Mode.Error);
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
    }

}