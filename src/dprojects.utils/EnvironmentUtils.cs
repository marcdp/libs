using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace DProjects.Utils {


    public static class EnvironmentUtils {


        //enums
        // ATENCIÓ: el package System.Runtime.Extensions implementa algunes dels metodes que anirien aqui


        //methods
        public static string GetCurrentFolder() {
            return Directory.GetCurrentDirectory();
        }
        public static string? GetUserFolder() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return System.Environment.GetEnvironmentVariable("HOMEDRIVE") + System.Environment.GetEnvironmentVariable("HOMEPATH");
            } else {
                return System.Environment.GetEnvironmentVariable("HOME");
            }
        }
        public static string? GetNetCoreSdkFolder() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return "C:\\Program Files\\dotnet\\sdk";
            } else {
                return null;
            }
        }
        public static bool IsWindows() {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        public static bool IsLinux() {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }
        public static bool IsOSX() {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
        public static string[] GetOsPlatformNames() {
            return ["windows", "linux", "osx"];
        }
        public static string GetOsPlatformName() {
            if (IsWindows()) return "windows";
            if (IsLinux()) return "linux";
            if (IsOSX()) return "osx";
            return "";
        }
        public static bool IsNetFramework() {
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.IndexOf("Framework") != -1;
        }

    }

}


