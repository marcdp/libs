using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;


namespace DProjects.Utils {


    public static class NetUtils {


        //ip utils
        public static string GetLocalIPAddress() {
            string hostName = Dns.GetHostName(); // Get the host name
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            foreach (var ip in addresses) {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
                {
                    return ip.ToString();
                }
            }
            return "No IPv4 address found";
        }
        public static string GetClientIP(TcpClient tcpClient) {
            try {
                PropertyInfo? prInfo = tcpClient.GetType().GetProperty("Client", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (prInfo != null) {
                    Socket? socket = (Socket?)prInfo.GetValue(tcpClient, null);
                    if (socket == null) return "";
                    return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                } else {
                    return "";
                }
            } catch (Exception) {
                return "";
            }
        }
        public static string GetClientIP2(TcpClient tcpClient) {
            try {
                PropertyInfo? prInfo = tcpClient.GetType().GetProperty("Server", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (prInfo != null) {
                    Socket? socket = (Socket?)prInfo.GetValue(tcpClient, null);
                    if (socket == null) return "";
                    return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                } else {
                    return "";
                }
            } catch (Exception) {
                return "";
            }
        }
        public static double ConvertIp4ToNumber(string ip) {
            string[] arr = ip.Split('.');
            double a = 0;
            double b = 0;
            double c = 0;
            double d = 0;
            a = Convert.ToDouble(arr[0]) * 256 * 256 * 256;
            b = Convert.ToDouble(arr[1]) * 256 * 256;
            c = Convert.ToDouble(arr[2]) * 256;
            d = Convert.ToDouble(arr[3]);
            return a + b + c + d;
        }
        public static IPAddress? ConvertHostNameToIpV4(string hostName) {
            IPHostEntry ipHostEntry = Dns.GetHostEntryAsync(hostName).Result;
            foreach (var ip in ipHostEntry.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip;
            }
            return null;
        }
        public static IPAddress? ConvertHostNameToIpV6(string hostName) {
            IPHostEntry ipHostEntry = Dns.GetHostEntryAsync(hostName).Result;
            foreach (var ip in ipHostEntry.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6) return ip;
            }
            return null;
        }


    }

}


