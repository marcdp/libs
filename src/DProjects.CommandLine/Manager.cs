using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DProjects.CommandLine.Schema;
using DProjects.Factories;
using DProjects.Utils;

using Microsoft.Extensions.DependencyInjection;

namespace DProjects.CommandLine {


    public class Manager(IServiceProvider services, Configuration configuration) {


        //methods
        private void ShowError(string message) {
            System.Console.Error.WriteLine(configuration + ": " + message);
        }
        private void ShowHelp() {
            var sb = new StringBuilder();
            //header
            sb.AppendLine($"usage: {configuration.AppName} COMMAND ");
            sb.AppendLine();
            //commands
            sb.AppendLine("commands:");
            foreach (var command in configuration.Commands.Values) {
                sb.AppendLine($"  {command.Name.Split('-').LastOrDefault(),-15} {command.Description}");
            }
            sb.AppendLine();
            //
            sb.AppendLine($"RUN '{configuration.AppName} --help' for more information on a command");
            //print
            System.Console.Out.Write(sb.ToString());
        }
        private void ShowHelp(CmdSchemaDefinition cmdSchemaDefinition) {
            var sb = new StringBuilder();
            //header
            sb.AppendLine($"usage: ");
            sb.AppendLine($"  {configuration.AppName} {cmdSchemaDefinition.Name}");
            //subcomands
            var subcommands = new List<string>();
            foreach (var subcommand in configuration.Commands.Values) {
                if (subcommand.Name.StartsWith(cmdSchemaDefinition.Name + "-")) {
                    subcommands.Add($"  {subcommand.Name.Split('-').LastOrDefault(),-15} {subcommand.Description}");
                }
            }
            if (subcommands.Count > 0) {
                sb.AppendLine();
                sb.AppendLine($"commands: ");
                foreach (var subcommand in subcommands) {
                    sb.AppendLine(subcommand);
                }
            }
            //details
            sb.Append(DefinitionToHelp.GetHelpText(cmdSchemaDefinition, !System.Console.IsOutputRedirected));
            //print
            System.Console.Out.Write(sb.ToString());
        }
        public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken) {

            // get command name
            CmdSchemaDefinition? cmdSchemaDefinition = null;
            if (configuration.Commands.Count == 1) {
                cmdSchemaDefinition = configuration.Commands.Values.First();  
            } else {
                for (var i=args.Length; i > 0; i--) {
                    var name = String.Join("-", args, 0, i);
                    if (configuration.Commands.TryGetValue(name, out var cmd)) {
                        cmdSchemaDefinition = cmd;
                        args = args.Skip(i).ToArray();
                        break;
                    }
                }
            }

            // if command not found
            if (cmdSchemaDefinition == null || cmdSchemaDefinition.Handler == null) {
                ShowHelp();
                return -1;
            }

            // check if help 
            if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help")) {
                if (cmdSchemaDefinition == null) {
                    ShowHelp();
                } else {
                    ShowHelp(cmdSchemaDefinition);
                }
                return 0;
            }

            // create command instance
            var instance = (ICommand) ActivatorUtilities.CreateInstance(services, cmdSchemaDefinition.Handler);

            // inject command properties
            var sheBangArgsSeparator = Guid.NewGuid().ToString();
            var errors = new List<string>();
            var defaults = new Dictionary<string, string>();
            cmdSchemaDefinition.InitializeObjectProperties(instance, args, null, sheBangArgsSeparator, defaults, errors, (type, key) => {
                //inject property values to command, from dependency injection container
                if (string.IsNullOrEmpty(key)) {
                    //get default service from container
                    return services.GetRequiredService(type);
                } else {
                    //get keyed service from container
                    var keyedServiceProvider = (IKeyedServiceProvider)services;
                    var result = keyedServiceProvider.GetKeyedService(type, key);
                    if (result != null) return result;
                    //get factory from container
                    var factoryType = typeof(IFactoryByUrl<>).MakeGenericType(type);
                    var factory = services.GetRequiredService(factoryType);
                    //create instance from container
                    var createMethodInfo = factory.GetType().GetMethod("Create")!;
                    result = createMethodInfo.Invoke(factory, [key]);
                    //return result
                    return result!;
                }
            });

            // show error for each argument not assigned to a property
            foreach (var error in errors) {
                await System.Console.Error.WriteLineAsync(error);
                return Errors.ERROR_INVALID_ARGUMENTS;
            }

            // execute
            int result = await instance.ExecuteAsync(cancellationToken);

            // return
            return result;
        }

    }

}