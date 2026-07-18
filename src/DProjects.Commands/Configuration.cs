using System.Collections.Generic;
using System.Linq;

using DProjects.Factories;

using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Commands {

    public class Configuration(IServiceCollection services, string appName) {

        //properties
        public string AppName { get; set; } = appName;
        public IServiceCollection Services => services;
        public IDictionary<string, Schema.CmdSchemaDefinition> Commands = new Dictionary<string, Schema.CmdSchemaDefinition>();

        //Add commands from assembly
        public void AddGlobalFlag(char code, string name, string description, string defaultValue) {
            
        }
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
        public void AddGlobalVirtualFlag(string name, string description, string? defaultValue) {
            // Add global virtual flag to all commands
            foreach (var command in Commands.Values) {
                command.AddGlobalVirtualFlag(name, description, defaultValue);
            }
        }

    }

}