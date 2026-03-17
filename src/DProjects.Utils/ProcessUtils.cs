using System.Diagnostics;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Utils {


    public static class ProcessUtils {


        //classes/structus
        public struct ProcessResult {
            public bool Completed;
            public int? ExitCode; 
            public string Output;
            public string Error;
        }


        //methods
        public static async Task<string> ExecuteCmdAsync(string cmd, CancellationToken cancellationToken) {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var result = await ExecuteProcessAsync("cmd.exe", $"/c \"{escapedArgs}\"", cancellationToken);
            return result.Output;
        }
        public static async Task<string> ExecuteBashAsync(string cmd, CancellationToken cancellationToken) {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var result = await ExecuteProcessAsync("/bin/bash", $"-c \"{escapedArgs}\"", cancellationToken);
            return result.Output;
        }
        //public static async Task<ProcessResult> ExecuteProcessAsync(string fileName, string arguments, CancellationToken cancellationToken) {
        //    using (var process = new Process()) {
        //        process.StartInfo.FileName = fileName;
        //        process.StartInfo.Arguments = arguments;
        //        process.StartInfo.UseShellExecute = false;
        //        process.StartInfo.RedirectStandardOutput = true;
        //        process.StartInfo.RedirectStandardError = true;

        //        var outputTask = process.StandardOutput.ReadToEndAsync();
        //        var errorTask = process.StandardError.ReadToEndAsync();

        //        var tcs = new TaskCompletionSource<ProcessResult>();

        //        cancellationToken.Register(() => {
        //            try {
        //                process.Kill();
        //            } catch { }
        //            tcs.TrySetCanceled();
        //        });

        //        process.Exited += (sender, e) => {
        //            tcs.TrySetResult(new ProcessResult {
        //                ExitCode = process.ExitCode,
        //                Output = outputTask.Result,
        //                Error = errorTask.Result
        //            });
        //        };

        //        process.Start();

        //        return await tcs.Task;
        //    }
        //}
        public static int ExecuteProcess(string filename, string workingfolder, string args, ref string strOutput, ref string strError, bool avoidOpenWindow = true) {
            int exitCode = 0;
            var process = default(System.Diagnostics.Process);
            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = filename;
            psi.CreateNoWindow = avoidOpenWindow;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = workingfolder;
            psi.Arguments = args;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            process = System.Diagnostics.Process.Start(psi);
            process.WaitForExit();
            exitCode = process.ExitCode;
            strOutput = process.StandardOutput.ReadToEnd();
            strError = process.StandardError.ReadToEnd();
            process.Dispose();
            return exitCode;
        }
        public static async Task<ProcessResult> ExecuteProcessAsync(string fileName, string arguments, CancellationToken cancellationToken) {
            var result = new ProcessResult();
            using (var process = new Process()) {
                //prepare
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                //output
                var outputBuilder = new StringBuilder();
                var outputCloseEvent = new TaskCompletionSource<bool>();
                process.OutputDataReceived += (s, e) => {
                    if (e.Data == null) {
                        outputCloseEvent.SetResult(true);
                    } else {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                //error
                var errorBuilder = new StringBuilder();
                var errorCloseEvent = new TaskCompletionSource<bool>();
                process.ErrorDataReceived += (s, e) => {
                    if (e.Data == null) {
                        errorCloseEvent.SetResult(true);
                    } else {
                        errorBuilder.AppendLine(e.Data);
                    }
                };
                //start
                bool isStarted;

                try {
                    isStarted = process.Start();
                } catch (Exception error) {
                    // Usually it occurs when an executable file is not found or is not executable
                    result.Completed = true;
                    result.ExitCode = -1;
                    result.Output = error.Message;
                    isStarted = false;
                }
                //wait
                if (isStarted) {
                    // Reads the output stream first and then waits because deadlocks are possible
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    // Creates task to wait for process exit using timeout
                    var waitForExit = Task.Run(() => {
                        process.WaitForExit();
                    }, cancellationToken);                    
                    // Create task to wait for process exit and closing all output streams
                    var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);
                    // Waits process completion and then checks it was not completed by timeout
                    if (await Task.WhenAny(processTask) == processTask && waitForExit.Status == TaskStatus.RanToCompletion ) {
                        result.Completed = true;
                        result.ExitCode = process.ExitCode;
                    } else {
                        // Kill hung process
                        try {
                            process.Kill();
                        } catch {
                        }
                    }
                    result.Output = outputBuilder.ToString();
                    result.Error = errorBuilder.ToString();
                }
            }
            return result;
        }
        public static void ShellExecute(string command, string verb = "open") {
            var startInfo = new System.Diagnostics.ProcessStartInfo(command);
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }
        public static void Kill(int pid) {
            var process = System.Diagnostics.Process.GetProcessById(pid);
            process.Kill();
        }
    }

}
