using System.Text.RegularExpressions;


namespace DProjects.Utils {


    public static class BrowserUtils {


        //object
        public class BrowserInfo {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public int Major { get; set; } = 0;
        }
        public class CpuInfo {
            public string Architecture { get; set; } = "";
        }
        public class DeviceInfo {
            public string Type { get; set; } = "";
            public string Model { get; set; } = "";
            public string Vendor { get; set; } = "";
        }
        public class OsInfo {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public int Major { get; set; } = 0;
        }
        public class Info {
            public BrowserInfo Browser { get; set; }
            public CpuInfo Cpu { get; set; }
            public DeviceInfo Device { get; set; }
            public OsInfo Os { get; set; }
            public Info(BrowserInfo browser, CpuInfo cpu, DeviceInfo device, OsInfo os) {
                Browser = browser;
                Cpu = cpu;
                Device = device;
                Os = os;
            }
        }


        public static string GetLanguageByAcceptLanguage(string acceptLanguageHeader) {
            acceptLanguageHeader ??= "";
            string result = "";
            foreach (string acceptLanguagePart in acceptLanguageHeader.Split(',')) {
                if (!string.IsNullOrEmpty(acceptLanguagePart)) {
                    var aux = acceptLanguagePart;
                    if (aux.IndexOf(";") != -1) {
                        aux = aux.Substring(0, aux.IndexOf(";"));
                    }
                    result = aux;
                    break;
                }
            }
            return result;
        }
        public static string GetPlatformByUserAgent(string userAgent) {
            userAgent ??= "";
            Regex isAndroidPlatformTest = new Regex("(android*)\\w*", RegexOptions.IgnoreCase);
            Regex isMobilePlatformTest = new Regex("(mobile*)\\w*", RegexOptions.IgnoreCase);
            Regex isIPhonePlatformTest = new Regex("(iphone;)\\w*", RegexOptions.IgnoreCase);
            Regex isIPadPlatformTest = new Regex("(ipad;)\\w*", RegexOptions.IgnoreCase);
            Regex isAndroidApp = new Regex("(AndroidApp)\\w*", RegexOptions.IgnoreCase);
            Regex isBlackBerryPlatformTest = new Regex("(BlackBerry)\\w*", RegexOptions.IgnoreCase);
            Regex isWindowsPhonePlatformTest = new Regex("(Windows Phone)\\w*", RegexOptions.IgnoreCase);
            string platformDetected = "desktop";
            if (isAndroidPlatformTest.IsMatch(userAgent)) {
                // We have an android device user agent
                if (isMobilePlatformTest.IsMatch(userAgent)) {
                    platformDetected = "mobile";
                } else if (isAndroidApp.IsMatch(userAgent)) {
                    platformDetected = "mobile";
                } else {
                    platformDetected = "tablet";
                }
            } else if (isBlackBerryPlatformTest.IsMatch(userAgent)) {
                platformDetected = "mobile";
            } else if (isWindowsPhonePlatformTest.IsMatch(userAgent)) {
                platformDetected = "mobile";
            } else if (isIPadPlatformTest.IsMatch(userAgent)) {
                platformDetected = "tablet";
            } else if (isIPhonePlatformTest.IsMatch(userAgent)) {
                platformDetected = "mobile";
            }
            return platformDetected;
        }


        //get info
        private static bool FillBrowserInfo(string userAgent, string keyword, string name, BrowserInfo browser) {
            var i = userAgent.IndexOf(keyword);
            if (i != -1) {
                browser.Name = name;
                browser.Version = userAgent.Substring(i + keyword.Length).Replace(' ',';').Split(';')[0];
                var aux = browser.Version;
                if (aux.IndexOf(".") != aux.LastIndexOf(".")) aux = aux.Substring(0, aux.IndexOf("."));
                if (float.TryParse(aux, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float major)) {
                    browser.Major = (int)major;
                }
                return true;
            }
            return false;
        }
        private static bool FillOsInfo(string userAgent, string keyword, string name, OsInfo os) {
            var i = userAgent.IndexOf(keyword);
            if (i != -1) {
                os.Name = name;
                os.Version = userAgent.Substring(i + keyword.Length).Replace(' ', ';').Replace('_', '.').Split(';')[0];
                var aux = os.Version;
                if (aux.IndexOf(".") != aux.LastIndexOf(".")) aux = aux.Substring(0, aux.IndexOf("."));
                if (float.TryParse(aux, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float major)) {
                    os.Major = (int)major;
                }
                return true;
            }
            return false;
        }
        public static Info GetInfo(string userAgent) {
            //browser
            var browser = new BrowserInfo();
            if (FillBrowserInfo(userAgent, "MSIE ", "MSIE", browser) || FillBrowserInfo(userAgent, "Trident/", "MSIE", browser)) {
            } else if (FillBrowserInfo(userAgent, "Firefox/", "Firefox", browser)) {
            } else if (FillBrowserInfo(userAgent, "Opera/", "Opera", browser) || FillBrowserInfo(userAgent, "OPR/", "Opera", browser)) {
            } else if (FillBrowserInfo(userAgent, "Edge/", "Edge", browser) || FillBrowserInfo(userAgent, "Edg/", "Edge", browser)) {
            } else if (FillBrowserInfo(userAgent, "Chrome/", "Chrome", browser)) {
            } else if (FillBrowserInfo(userAgent, "Safari/", "Safari", browser) || FillBrowserInfo(userAgent, "Darwin/", "Safari", browser)) {
                FillBrowserInfo(userAgent, "Version/", "Safari", browser);
            } else if (userAgent.IndexOf("bot/")!=-1 || userAgent.IndexOf("bot.") != -1 || userAgent.IndexOf("bot ") != -1 || userAgent.IndexOf("Bot/") != -1 || userAgent.IndexOf("Bot.") != -1 || userAgent.IndexOf("Bot ") != -1) {
                browser.Name = "bot";
            }
            //cpu
            var cpu = new CpuInfo ();
            //device
            var device = new DeviceInfo ();
            if (userAgent.IndexOf("iPad") != -1) {
                device.Type = "Tablet";
                device.Model = "iPad";
            } else if (userAgent.IndexOf("kindle") != -1) {
                device.Type = "Tablet";
                device.Model = "Kindle";
            } else if (userAgent.IndexOf("iPhone") != -1) {
                device.Type = "Mobile";
                device.Model = "iPhone";
            } else if (userAgent.IndexOf("Mobile") != -1) {
                device.Type = "Mobile";
                device.Model = "";
            } else if (userAgent.IndexOf("Tablet") != -1) {
                device.Type = "Tablet";
                device.Model = "";
            }
            //os
            var os = new OsInfo ();
            if (FillOsInfo(userAgent, "Windows NT ", "Windows", os)) {
            } else if (FillOsInfo(userAgent, "Android ", "Android", os)) {
            } else if (FillOsInfo(userAgent, "iPhone OS ", "IOS", os) || FillOsInfo(userAgent, "Darwin/", "IOS", os)) {
            } else if (FillOsInfo(userAgent, "iPhone; CPU OS ", "IOS", os)) {
            } else if (FillOsInfo(userAgent, "iPad; CPU OS ", "IOS", os) || FillOsInfo(userAgent, "Darwin/", "IOS", os)) {
            } else if (FillOsInfo(userAgent, "Mac OS X ", "MacOS", os)) {
            } else if (FillOsInfo(userAgent, "Linux ", "Linux", os)) {
            } else if (FillOsInfo(userAgent, "CrOS ", "ChromeOS", os)) {
            }
            //return
            return new Info(browser, cpu, device, os);
        }
    }




}


