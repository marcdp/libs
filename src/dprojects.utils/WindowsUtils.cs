using System;
using System.Runtime.InteropServices;

namespace DProjects.Utils {


    public static class WindowsUtils {


        //constants
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

        //declarations
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();


        //declaration
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        //declaration
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int NetGetJoinInformation(string? server, out IntPtr domain, out NetJoinStatus status);
        [DllImport("Netapi32.dll")]
        static extern int NetApiBufferFree(IntPtr Buffer);
        public enum NetJoinStatus {
            NetSetupUnknownStatus = 0,
            NetSetupUnJoined,
            NetSetupWorkgroupName,
            NetSetupDomainName
        }

        //methods
        public static bool EnableVirtualTerminalOutputProcessing() {
            if (!EnvironmentUtils.IsWindows()) return false;

            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode)) return false;
            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING; // | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode)) return false;

            return true;
        }
        public static bool EnableVirtualTerminalInputProcessing() {
            if (!EnvironmentUtils.IsWindows()) return false;

            var iStdIn = GetStdHandle(STD_INPUT_HANDLE);
            if (!GetConsoleMode(iStdIn, out uint inConsoleMode)) return false;
            inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
            if (!SetConsoleMode(iStdIn, inConsoleMode)) return false;

            return true;
        }
        public static bool IsUserAnAdministrator() {
            return IsUserAnAdmin();
        }
        public static string GetWorkgroupName() {
            int result = 0;
            string? domain = null;
            IntPtr pDomain = IntPtr.Zero;
            NetJoinStatus status = NetJoinStatus.NetSetupUnknownStatus;
            try {
                result = NetGetJoinInformation(null, out pDomain, out status);
                if (result == 0 && status == NetJoinStatus.NetSetupDomainName) {
                    domain = Marshal.PtrToStringAuto(pDomain);
                } else if (result == 0 && status == NetJoinStatus.NetSetupWorkgroupName) {
                    domain = Marshal.PtrToStringAuto(pDomain);
                }
            } catch (System.DllNotFoundException) {
                return "";
            } finally {
                if (pDomain != IntPtr.Zero) NetApiBufferFree(pDomain);
            }
            if (domain == null) domain = "";
            return domain;
        }


    }

}


