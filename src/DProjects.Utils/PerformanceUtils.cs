using System.Diagnostics;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace DProjects.Utils {


    public static class PerformanceUtils {

        // declares
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);


        // static vars
        private static readonly int ProcessorCount = Environment.ProcessorCount;
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();

        // methods

        public static async Task<double> GetCpuUsageAsync(CancellationToken cancellationToken) {
            var startCpuTime = CurrentProcess.TotalProcessorTime;
            var startTime = DateTime.UtcNow;
            await Task.Delay(500, cancellationToken); // sampling interval
            CurrentProcess.Refresh();
            var endCpuTime = CurrentProcess.TotalProcessorTime;
            var endTime = DateTime.UtcNow;
            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds * ProcessorCount;
            var cpuUsage = cpuUsedMs / totalMsPassed; // fraction
            return cpuUsage;
        }
        public static Task<long> GetMemoryUsedAsync(CancellationToken cancellationToken) {
            var memUsed = CurrentProcess.WorkingSet64;
            return Task.FromResult(memUsed);
        }
        public static Task<long> GetPrivateMemoryUsedAsync(CancellationToken cancellationToken) {
            var memUsed = CurrentProcess.PrivateMemorySize64;
            return Task.FromResult(memUsed);
        }
        public static Task<double> GetMemoryUsageAsync(CancellationToken cancellationToken) {
            var memUsed = CurrentProcess.WorkingSet64;
            var memTotal = GetTotalMemoryBytes();
            var memUsage = memTotal > 0 ? (double)memUsed / memTotal : 0;
            return Task.FromResult(memUsage);
        }
        public static ulong GetTotalMemoryBytes() {
            try {
                if (EnvironmentUtils.IsWindows()) {
                    MEMORYSTATUSEX memStatus = new();
                    memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                    if (GlobalMemoryStatusEx(ref memStatus)) return memStatus.ullTotalPhys;
                } else if (EnvironmentUtils.IsLinux()) {
                    var lines = System.IO.File.ReadAllLines("/proc/meminfo");
                    foreach (var line in lines) { 
                        if (line.StartsWith("MemTotal:")) {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (ulong.TryParse(parts[1], out var kB)) return kB * 1024;
                        }
                    }
                } else if (EnvironmentUtils.IsOSX()) {
                    using var psi = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "sysctl",
                            Arguments = "-n hw.memsize",
                            RedirectStandardOutput = true
                        }
                    };
                    psi.Start();
                    var output = psi.StandardOutput.ReadToEnd();
                    psi.WaitForExit();
                    if (ulong.TryParse(output.Trim(), out var bytes)) {
                        return bytes;
                    }
                }
            } catch { }
            return 0;
        }
    }
}