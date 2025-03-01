using System.Collections.Generic;
using System.Linq;

using DProjects.Factories;

using Microsoft.Extensions.DependencyInjection;

namespace DProjects.CommandLine {

    public class Configuration(IServiceCollection services, string appName) {

        //properties
        public string AppName { get; set; } = appName;
        public IServiceCollection Services => services;
        public IDictionary<string, Schema.CmdSchemaDefinition> Commands = new Dictionary<string, Schema.CmdSchemaDefinition>();

        //Add commands from assembly
        public void AddCommandsFromAssembly<TAssembly>() where TAssembly : IAssembly {
            AddCommandsFromAssembly(typeof(TAssembly).Assembly);
        }
        public void AddCommandsFromAssembly(System.Reflection.Assembly assembly) {
            foreach (var type in assembly.GetTypes().Where(x => typeof(ICommand).IsAssignableFrom(x))) {
                var location = type.FullName;
                var commandType = "command";
                var addHelpTag = true;
                var module = "";
                var command = Schema.CmdSchemaDefinition.Create(type, location, commandType, addHelpTag, module);
                Commands.Add(command.Name, command);
            }
        }

    }

}