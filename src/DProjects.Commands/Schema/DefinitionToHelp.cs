
using DProjects.Utils;
using System;
using System.Text;

namespace DProjects.Commands.Schema {

    public class DefinitionToHelp {


        public static string GetHelpText(CmdSchemaDefinition command, bool useAnsiCodes) {
            var result = new StringBuilder();
            int indent = 25; 
            //first empty line
            result.AppendLine(""); 
            //location
            var assembly = command.Handler?.Assembly;
            result.AppendLine("location:");
            result.AppendLine("  " + (string.IsNullOrEmpty(command.Location) ? command.Name: command.Location) + " (" + command.Type + ", " + command.Module + ")");
            result.AppendLine("");
            //category
            result.AppendLine("category:");
            result.AppendLine("  " + command.Category);
            result.AppendLine("");
            //description
            result.AppendLine("description:");
            if (command.Description != null) {
                foreach (string line in command.Description.Replace("" + CharUtils.CHAR_CR, "").Split(CharUtils.CHAR_LF)) {
                    result.AppendLine("  " + line);
                }
            } else {
                result.AppendLine("  " + command);
            }
            result.AppendLine("");
            //synopsis
            result.AppendLine("synopsis:");
            if (command.Synopsis.Length > 0) {
                foreach (var synopsis in command.Synopsis) {
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_YELLOW);
                    result.AppendLine("  # " + (synopsis.Description.Length > 0 ? synopsis.Description : "Synopsis"));
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_WHITE);
                    foreach (var line in synopsis.Example.Split('\n')) {
                        result.AppendLine("  " + line);
                    }
                }
                result.AppendLine("");
            } else {
                StringBuilder usage = new StringBuilder();
                usage.Append("  ").Append(command.Name);
                if (command.Flags.Length > 0) {
                    usage.Append(" [FLAGS]");
                }
                foreach (var argument in command.Arguments) {
                    var aDefault = argument.Default;
                    var required = argument.Required;
                    usage.Append(" " + (aDefault != null ? "[" : "") + (argument.Alias ?? argument.Name).ToUpper() + (aDefault != null ? "]" : ""));
                }
                result.AppendLine(usage.ToString());
                result.AppendLine("");
            }
            //flags
            if (command.Flags.Length > 0) {
                var resultInherited = new StringBuilder();
                result.AppendLine("flags:");
                foreach (var flag in command.Flags) {
                    var sb = new StringBuilder();                    
                    sb.Append("  " + string.Format("{0,-" + indent + "} ", (flag.Char == '\0' ? "     " : "-" + flag.Char + ",  ") + "--" + (flag.Alias ?? StringUtils.CamelToKebabCase(flag.Name, true))));
                    if (useAnsiCodes) sb.Append(ConsoleUtils.COLOR_YELLOW);
                    sb.Append(" # ");
                    if (flag.Description.Length > 0) sb.Append(flag.Description + ". ");
                    sb.Append(flag.Type + ". ");
                    if (!flag.Required) sb.Append("Optional. Default '" + flag.Default + "'. ");
                    if (flag.Domain?.Length > 0) sb.Append("(" + String.Join(", ", flag.Domain) + ") ");
                    if (useAnsiCodes) sb.Append(ConsoleUtils.COLOR_WHITE);
                    sb.AppendLine("");
                    if (flag.PropertyInfo != null && flag.PropertyInfo.DeclaringType != command.Handler) {
                        resultInherited.Append(sb.ToString());
                    } else {
                        result.Append(sb.ToString());
                    }                    
                }                
                result.AppendLine("");
                if (resultInherited.Length > 0) {
                    result.AppendLine("flags inherited:");
                    result.Append(resultInherited.ToString());
                    result.AppendLine("");
                }
            }
            //arguments
            if (command.Arguments.Length > 0) {
                result.AppendLine("arguments:  ");
                foreach (var argument in command.Arguments) {
                    result.Append("  " + string.Format("{0,-" + indent + "}", (!argument.Required ? "[" : "") + (argument.Alias ?? argument.Name).ToUpper() + (!argument.Required ? "]" : "")) + " ");
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_YELLOW);
                    result.Append(" # ");
                    if (argument.Description.Length > 0) result.Append(argument.Description + ". ");
                    result.Append(argument.Type + ". ");
                    if (!argument.Required) result.Append("Optional. Default '" + argument.Default + "'. ");
                    if (argument.Domain?.Length > 0) result.Append("(" + String.Join(", ", argument.Domain) + ") ");
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_WHITE);
                    result.AppendLine("");
                }
                result.AppendLine("");
            }
            //Body
            if (command.Body != null) {
                result.AppendLine("body:  ");
                var aux = new StringBuilder();
                result.Append("  " + string.Format("{0,-" + indent + "}", "Body") + " ");
                if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_YELLOW);
                result.Append(" # ");
                if (command.Body.Description.Length > 0) result.Append(command.Body.Description + ". ");
                if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_WHITE);
                result.AppendLine("");
                result.AppendLine("");
            }
            //exit codes
            if (command.ExitCodes.Length > 0) {
                result.AppendLine("  Exit codes:");
                foreach (var exitCode in command.ExitCodes) {
                    result.Append("      " + String.Format("{0,-" + indent + "}  ", exitCode.Code));
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_YELLOW);
                    result.Append(exitCode.Description);
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_WHITE);
                    result.AppendLine("");
                }
                result.AppendLine("");
            }
            //help
            if (command.Help.Length > 0) {
                result.AppendLine("help:");
                foreach (string line in command.Help.Replace("" + CharUtils.CHAR_CR, "").Split(CharUtils.CHAR_LF)) {
                    result.AppendLine("      " + line);
                }
                result.AppendLine("");
            }
            //examples
            if (command.Examples.Length > 0) {
                result.AppendLine("examples:");
                foreach (var example in command.Examples) {
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_YELLOW);
                    result.AppendLine("      # " + (example.Description.Length > 0 ? example.Description : "Example"));
                    if (useAnsiCodes) result.Append(ConsoleUtils.COLOR_WHITE);
                    foreach (var line in example.Example.Split('\n')) {
                        result.AppendLine("      " + line);
                    }
                    result.AppendLine("");
                }
            }
            //help
            if (command.Tags.Length > 0) {
                result.AppendLine("tags:");
                foreach (var tag in command.Tags) {
                    result.AppendLine("      " + tag.Tag);
                }
                result.AppendLine("");
            }
            //return
            return result.ToString();
        }

    }
}
