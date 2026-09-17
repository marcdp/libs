using DProjects.Commands;
using DProjects.Commands.Attributes;


namespace DProjects.XShell.Commands {
    [Description("Launch a web server")]
    public class CommandAux() : ICommand {
        public async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
            return 0;
        }
    }

}
