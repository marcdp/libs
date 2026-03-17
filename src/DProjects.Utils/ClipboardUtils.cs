using System;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class ClipboardUtils {

        //minify css
        public static async Task SetTextAsync(string text, CancellationToken cancellationToken) {
            var tmpFile = FileUtils.WriteTempFile(text, new System.Text.UTF8Encoding(false));
            if (EnvironmentUtils.IsWindows()) {
                await ProcessUtils.ExecuteCmdAsync($"type {tmpFile} | clip", cancellationToken);
            } else if (EnvironmentUtils.IsOSX()) {
                await ProcessUtils.ExecuteBashAsync($"cat {tmpFile} | pbcopy", cancellationToken);
            } else if (EnvironmentUtils.IsLinux()) {
                await ProcessUtils.ExecuteBashAsync($"cat {tmpFile} | xclip -selection clipboard -i", cancellationToken);
            }
            FileUtils.DeleteFile(tmpFile);
        }
        public static async Task<string> GetTextAsync(CancellationToken cancellationToken) {
            var output = "";
            if (EnvironmentUtils.IsWindows()) {
                //status = await environment.ExecuteAsync("powershell -c 'get-clipboard'", out output, out string error);
            } else if (EnvironmentUtils.IsOSX()) {
                output = await ProcessUtils.ExecuteBashAsync("pbpaste", cancellationToken);
            } else if (EnvironmentUtils.IsLinux()) {
                output = await ProcessUtils.ExecuteBashAsync("xclip -selection clipboard -o", cancellationToken);
            }
            return output;
        }

    }


}


