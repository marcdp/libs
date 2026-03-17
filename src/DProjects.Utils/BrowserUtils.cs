using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace DProjects.Utils {


    public class BrowserUtils {


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


        //static methods
        //public static bool IsSearchEngine(string useragent) {
        //    if (useragent == null) {
        //        return false;
        //    }
        //    bool result = false;
        //    useragent = useragent.ToLower();
        //    if (useragent.IndexOf("aolbuild") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("ask.com") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("baidu") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("bingbot") != -1 || useragent.IndexOf("bingpreview") != -1 || useragent.IndexOf("msnbot") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("blekko") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("duck duck") != -1 || useragent.IndexOf("duckduckgo") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("adsbot-google") != -1 || useragent.IndexOf("googlebot") != -1 || useragent.IndexOf("mediapartners-google") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("teoma") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("yahoo") != -1 || useragent.IndexOf("slurp") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("crawler") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("robot") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("yandex") != -1) {
        //        result = true;
        //    }
        //    return result;
        //}
        //public static bool IsSmartphone(string useragent) {
        //    if (useragent == null) {
        //        return false;
        //    }
        //    bool result = false;
        //    useragent = useragent.ToLower();
        //    if (useragent.IndexOf("blackberry") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("android") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("iphone") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("mobile") != -1) {
        //        result = true;
        //    }
        //    if (useragent.IndexOf("ipad") != -1) {
        //        result = false;
        //    }
        //    return result;
        //}
        //public static bool IsWebBrowser(string userAgent) {
        //    if (userAgent == null) {
        //        return false;
        //    }
        //    if (userAgent.IndexOf("Firefox") != -1) {
        //        return true;
        //    }
        //    if (userAgent.IndexOf("AppleWebKit") != -1) {
        //        return true;
        //    }
        //    if (userAgent.IndexOf("Mozilla") != -1) {
        //        return true;
        //    }
        //    if (userAgent.IndexOf("Chrome") != -1) {
        //        return true;
        //    }
        //    if (userAgent.IndexOf("Safari") != -1) {
        //        return true;
        //    }
        //    if (userAgent.IndexOf("Edge") != -1) {
        //        return true;
        //    }
        //    return true;
        //}
        //public static bool IsTablet(string useragent) {
        //    if (useragent == null) {
        //        return false;
        //    }
        //    bool result = false;
        //    useragent = useragent.ToLower();
        //    if (useragent.IndexOf("ipad") != -1) {
        //        result = true;
        //    }
        //    return result;
        //}
        //public static bool IsIE(string useragent) {
        //    if (useragent == null) {
        //        return false;
        //    }
        //    if (useragent.IndexOf("MSIE") != -1) {
        //        return true;
        //    }
        //    return false;
        //}
        //public static string GetBrowserByUserAgent(string useragent) {
        //    if (useragent == null) {
        //        return "";
        //    }
        //    string agentPart = "";
        //    string version = "";
        //    if (useragent.Contains("MSIE 5.0")) {
        //        return "Internet Explorer 5.0";
        //    } else if (useragent.Contains("MSIE 6.0")) {
        //        return "Internet Explorer 6.0";
        //    } else if (useragent.Contains("MSIE 7.0")) {
        //        return "Internet Explorer 7.0";
        //    } else if (useragent.Contains("MSIE 8.0")) {
        //        return "Internet Explorer 8.0";
        //    } else if (useragent.Contains("MSIE 9.0")) {
        //        return "Internet Explorer 9.0";
        //    } else if (useragent.Contains("MSIE 10.0")) {
        //        return "Internet Explorer 10.0";
        //    } else if (useragent.Contains("Firefox")) {
        //        return useragent.Substring(useragent.IndexOf("Firefox")).Replace("/", " ");
        //    } else if (useragent.Contains("Opera")) {
        //        return useragent.Substring(useragent.IndexOf("Opera"));
        //    } else if (useragent.Contains("Edge")) {
        //        agentPart = useragent.Substring(useragent.IndexOf("Edge"));
        //        return agentPart.Substring(0, agentPart.IndexOf("Edge") - 1).Replace("/", " ");
        //    } else if (useragent.Contains("Chrome")) {
        //        agentPart = useragent.Substring(useragent.IndexOf("Chrome"));
        //        return agentPart.Substring(0, agentPart.IndexOf("Safari") - 1).Replace("/", " ");
        //    } else if (useragent.Contains("Safari")) {
        //        if (useragent.IndexOf("Version") != -1) {
        //            agentPart = useragent.Substring(useragent.IndexOf("Version"));
        //            version = agentPart.Substring(0, agentPart.IndexOf("Safari") - 1).Replace("Version/", "");
        //        } else if (useragent.IndexOf("Safari/") != -1) {
        //            agentPart = useragent.Substring(useragent.IndexOf("Safari/"));
        //            version = agentPart.Substring(agentPart.IndexOf("Safari/") + 7, agentPart.Length - (agentPart.IndexOf("Safari/") + 7));
        //        } else {
        //            version = "Unknown";
        //        }
        //        return "Safari " + version;
        //    } else if (useragent.Contains("Konqueror")) {
        //        agentPart = useragent.Substring(useragent.IndexOf("Konqueror"));
        //        return agentPart.Substring(0, agentPart.IndexOf(";")).Replace("/", " ");
        //    } else if (useragent.ToLower().Contains("bot") || useragent.ToLower().Contains("search") || useragent.ToLower().Contains("spider")) {
        //        return "Search Bot";
        //    }
        //    return "";
        //}
        public static string GetLanguageByAcceptLanguage(string acceptLanguageHeader) {
            if (acceptLanguageHeader == null) {
                acceptLanguageHeader = "";
            }
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
        public static string GetPlatformByUserAgent(string useragent) {
            if (useragent == null) {
                useragent = "";
            }
            Regex isAndroidPlatformTest = new Regex("(android*)\\w*", RegexOptions.IgnoreCase);
            Regex isMobilePlatformTest = new Regex("(mobile*)\\w*", RegexOptions.IgnoreCase);
            Regex isIphonePlatformTest = new Regex("(iphone;)\\w*", RegexOptions.IgnoreCase);
            Regex isIpadPlatformTest = new Regex("(ipad;)\\w*", RegexOptions.IgnoreCase);
            Regex isAndroidApp = new Regex("(AndroidApp)\\w*", RegexOptions.IgnoreCase);
            Regex isBlackBerryPlatformTest = new Regex("(BlackBerry)\\w*", RegexOptions.IgnoreCase);
            Regex isWindowsPhonePlatformTest = new Regex("(Windows Phone)\\w*", RegexOptions.IgnoreCase);
            string platformDetected = "desktop";
            if (isAndroidPlatformTest.IsMatch(useragent)) {
                // We have an android device user agent
                if (isMobilePlatformTest.IsMatch(useragent)) {
                    platformDetected = "mobile";
                } else if (isAndroidApp.IsMatch(useragent)) {
                    platformDetected = "mobile";
                } else {
                    platformDetected = "tablet";
                }
            } else if (isBlackBerryPlatformTest.IsMatch(useragent)) {
                platformDetected = "mobile";
            } else if (isWindowsPhonePlatformTest.IsMatch(useragent)) {
                platformDetected = "mobile";
            } else if (isIpadPlatformTest.IsMatch(useragent)) {
                platformDetected = "tablet";
            } else if (isIphonePlatformTest.IsMatch(useragent)) {
                platformDetected = "mobile";
            }
            return platformDetected;
        }
        //private class BrowserOSTest {
        //    public string OS;
        //    public string[] FirstOccurrenceOfOSInUserAgentString;
        //    public string[] LastOccurrenceOfOSInUserAgentString;
        //    public BrowserOSTest(string OS, string[] firstOccurrence, string[] lastOccurrence) {
        //        this.OS = OS;
        //        this.FirstOccurrenceOfOSInUserAgentString = firstOccurrence;
        //        this.LastOccurrenceOfOSInUserAgentString = lastOccurrence;
        //    }
        //    public string GetNameAndVersionByUserAgent(string user_agent) {
        //        string result = "";
        //        user_agent = user_agent.ToLower();
        //        try {
        //            for (int i = 0; i <= FirstOccurrenceOfOSInUserAgentString.Length; i++) {
        //                string startKeyToSearch = FirstOccurrenceOfOSInUserAgentString[i].ToLower();
        //                string endKeyToSearch = LastOccurrenceOfOSInUserAgentString[i].ToLower();
        //                if (user_agent.IndexOf(startKeyToSearch) > -1 && user_agent.IndexOf(endKeyToSearch, user_agent.IndexOf(startKeyToSearch)) > -1) {
        //                    return user_agent.Substring(user_agent.IndexOf(startKeyToSearch), user_agent.IndexOf(endKeyToSearch, user_agent.IndexOf(startKeyToSearch)) - user_agent.IndexOf(startKeyToSearch));
        //                }
        //            }
        //        } catch (Exception) {
        //        }
        //        return result;
        //    }
        //}
        //public static string GetOsByUserAgent(string useragent) {
        //    if (useragent == null) {
        //        useragent = "";
        //    }
        //    List<BrowserOSTest> browserOSTests = new List<BrowserOSTest>();
        //    browserOSTests.Add(new BrowserOSTest("Windows", new[] { "windows", "windows" }, new[] { ";", ")" }));
        //    browserOSTests.Add(new BrowserOSTest("X11", new[] { "linux", "linux", "cros" }, new[] { ";", ")", " " }));
        //    browserOSTests.Add(new BrowserOSTest("Macintosh", new[] { "intel", "intel", "ppc", "ppc" }, new[] { ";", ")", ";", ")" }));
        //    browserOSTests.Add(new BrowserOSTest("Blackberry", new[] { "blackberry ", "blackberry" }, new[] { ";", "/" }));
        //    browserOSTests.Add(new BrowserOSTest("Android", new[] { "android" }, new[] { ";" }));
        //    browserOSTests.Add(new BrowserOSTest("Iphone", new[] { "iphone os", "iphone; opera", "cpu os" }, new[] { "like", "/", "l" }));
        //    browserOSTests.Add(new BrowserOSTest("Ipad", new[] { "cpu os" }, new[] { "like" }));
        //    string result = "unknown";
        //    try {
        //        if (useragent.IndexOf("(") > -1) { // Case (...)
        //            if (useragent.IndexOf(")") > -1) {
        //                useragent = useragent.Substring(useragent.IndexOf("(", 0), useragent.IndexOf(")") - useragent.IndexOf("(", 0) + 1);
        //            } else {
        //                useragent = useragent.Substring(useragent.IndexOf("(", 0), useragent.IndexOf(";", useragent.ToLower().IndexOf("windows")) - useragent.IndexOf("(", 0) + 1);
        //            }
        //        } else if (useragent.IndexOf(" ") != -1) { // Case Blackberry
        //            useragent = useragent.Substring(0, useragent.IndexOf(" "));
        //        }
        //    } catch (Exception) {
        //    }
        //    foreach (BrowserOSTest browserOSTest in browserOSTests) {
        //        if (useragent.ToLower().IndexOf(browserOSTest.OS.ToLower()) != -1) {
        //            result = browserOSTest.GetNameAndVersionByUserAgent(useragent);
        //            if (browserOSTest.OS.ToLower().Equals("iphone")) {
        //                result = result.Replace("cpu os", "iphone os");
        //                result = result.Replace("iphone; opera", "iphone os");
        //                result = result.Replace("_", ".");
        //            }
        //            if (browserOSTest.OS.ToLower().Equals("ipad")) {
        //                result = result.Replace("cpu os", "iphone os");
        //                result = result.Replace("_", ".");
        //            }
        //            if (result.ToLower().IndexOf("armv") > -1) {
        //                result = "android";
        //            }
        //            if (browserOSTest.OS.ToLower().Equals("macintosh")) {
        //                result = result.Replace("_", ".");
        //            }
        //            if (result.ToLower().Equals("cros")) {
        //                result = result.Replace("cros", "Chrome OS");
        //            }
        //            if (browserOSTest.OS.ToLower().Equals("windows")) {
        //                result = result.ToLower().Replace("windows phone os", "windows phone");
        //            }
        //            if (result.ToLower().IndexOf("linux") > -1) {
        //                result = result.ToLower().Replace("linux x86_64", "linux 86");
        //                result = result.ToLower().Replace("linux i686", "linux 86");
        //                result = result.ToLower().Replace("linux zbov", "linux 86");
        //            }
        //            if (result.IndexOf(";") != -1) {
        //                result = result.Substring(0, result.IndexOf(";"));
        //            }
        //            break;
        //        }
        //    }
        //    return result;
        //}


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


