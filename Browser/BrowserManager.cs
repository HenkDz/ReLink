/********************************************************************
Copyright (c) Shameel Ahmed.  All rights reserved.
********************************************************************/

using ReLink.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ReLink {
    internal class BrowserManager {

        static BrowserInfo[] browsers;
        static List<Rule> rules;

        static BrowserManager() {
            InitBrowsers();
            InitRules();
        }

        private static void InitBrowsers()
        {
            browsers = BrowserManager.GetRegisteredBrowsers();

            // Log the browsers array to check its contents
            foreach (var browser in browsers)
            {
                Console.WriteLine($"Browser: {browser.Name}, Type: {browser.BrowserType}, ExePath: {browser.ExePath}");
            }

            // Edge (if installed) quirk
            var edgeLegacy = GetBrowserByType(BrowserType.EdgeLegacy);
            if (edgeLegacy != null)
            {
                edgeLegacy.ExePath = "microsoft-edge:";
                edgeLegacy.ExePath = "microsoft-edge:";
            }
            else
            {
                Console.WriteLine("Edge Legacy browser not found.");
            }
        }

        private static void InitRules() {
            rules = new List<Rule>();
            if (BrowserSettings.Rules != null) {
                foreach (object objRule in BrowserSettings.Rules) {
                    Rule rule = Rule.Parse(Convert.ToString(objRule));
                    if (rule != null) {
                        rules.Add(rule);
                    }
                }
            }

            rules.Sort();
        }

        static internal BrowserInfo[] Browsers {
            get {
                return browsers;
            }
        }

        static internal BrowserInfo LaunchUrl(string url) {
            BrowserInfo browser = BrowserSettings.UseDefaultBrowserForAllLinks ?
                                    GetBrowserByName(BrowserSettings.DefaultBrowserName) :
                                    GetBrowserForUrl(url);

            LaunchUrlWithBrowser(url, browser);
            return browser;
        }

        static internal void LaunchUrlWithBrowser(string url, string browserName) {
            LaunchUrlWithBrowser(url, GetBrowserByName(browserName));
        }

        static internal void LaunchUrlWithBrowser(string url, BrowserInfo browser) {
            url = GetSanitizedUrl(url);

            ProcessStartInfo psi = new ProcessStartInfo();
            if (browser.ExePath.EndsWith(":")) {
                psi.FileName = $"{browser.ExePath}{url}";
            } else {
                psi.FileName = browser.ExePath;
                psi.Arguments = url;
            }

            new Process() {
                StartInfo = psi
            }.Start();
        }

        static BrowserInfo GetBrowserForUrl(string url) {
            BrowserInfo browser = null;

            url = GetSanitizedUrl(url);

            Rule rule = rules.FirstOrDefault(r => r.IsMatch(url));

            if (rule != null) {
                browser = GetBrowserByName(rule.BrowserName);
            }

            if (browser == null) {
                browser = GetBrowserByName(BrowserSettings.DefaultBrowserName);
            }

            if (browser == null) {
                browser = BrowserManager.GetBrowserByType(BrowserType.Edge);
                if (browser == null) {
                    browser = BrowserManager.GetBrowserByType(BrowserType.EdgeLegacy);
                    if (browser == null) {
                        browser = BrowserManager.GetBrowserByType(BrowserType.InternetExplorer);
                    }
                }
            }

            return browser;
        }

        internal static string GetSanitizedUrl(string url) {
            url = url.Trim();
            while (url.EndsWith("/")) {
                url = url.Substring(0, url.Length - 1);
            }

            return url;
        }

        static BrowserInfo GetBrowserByType(BrowserType browserType) {
            return browsers.FirstOrDefault(b => b.BrowserType == browserType);
        }

        internal static BrowserInfo GetBrowserByName(string browserName) {
            return browsers.FirstOrDefault(b => b.Name.Equals(browserName, StringComparison.OrdinalIgnoreCase));
        }

        internal static void RegisterOrUnregisterAsAdmin(bool register) {
#if (DEBUG)
            if (register) {
                RegisterRelinkAsBrowser();
            } else {
                UnregisterRelinkAsBrowser();
            }
#else
            new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = register ? ReLinkMain.ARG_REGISTER : ReLinkMain.ARG_UNREGISTER,
                    Verb = ReLinkMain.RUNAS_VERB
                }
            }.Start();
#endif
        }

        internal static void GetBrowserInfoFromAppId(string appId, out BrowserType browserType, out string browserName) {
            // Log the appId to see what IDs are being returned
            Console.WriteLine($"Detected App ID: {appId}");

            if (string.IsNullOrWhiteSpace(appId) || appId.Contains("IE.HTTP")) {
                browserType = BrowserType.InternetExplorer;
                browserName = "Internet Explorer";
            } else if (appId.Contains("AppXq0fevzme2pys62n3e0fbqa7peapykr8v")) {
                browserType = BrowserType.EdgeLegacy;
                browserName = "Edge (Legacy)";
            } else if (appId.Contains("MSEdgeHTM")) {
                browserType = BrowserType.Edge;
                browserName = "Edge";
            } else if (appId.StartsWith("FirefoxURL-")) {
                // Check for specific Firefox App IDs
                if (appId.Contains("CA9422711")) {
                    browserType = BrowserType.Firefox;
                    browserName = "Firefox Dev Edition";
                } else if (appId.Contains("308046B0A")) {
                    browserType = BrowserType.Firefox;
                    browserName = "Firefox";
                } else {
                    browserType = BrowserType.Firefox;
                    browserName = "Firefox (Unknown Version)";
                }
            } else if (appId.Contains("Chrome")) {
                // Check for specific Chrome App IDs
                if (appId.Contains("BHTM")) {
                    browserType = BrowserType.Chrome;
                    browserName = "Chrome Beta";
                } else {
                    browserType = BrowserType.Chrome;
                    browserName = "Chrome";
                }
            } else if (appId.Contains("BraveHTML")) {
                browserType = BrowserType.Brave;
                browserName = "Brave";
            } else if (appId.Contains("VivaldiHTM")) {
                browserType = BrowserType.Unknown;
                browserName = "Vivaldi";
            } else {
                browserType = BrowserType.Unknown;
                browserName = appId; // Set the browser name to the appId for logging
            }
        }

        private static BrowserRegistrar Registrar {
            get {
                return BrowserRegistrar.GetRegistrar();
            }
        }

        internal static BrowserInfo[] GetRegisteredBrowsers() => Registrar.GetRegisteredBrowsers();

        internal static void RegisterRelinkAsBrowser() => Registrar.RegisterRelinkAsBrowser();

        internal static void UnregisterRelinkAsBrowser() => Registrar.UnregisterRelinkAsBrowser();

        internal static void SetRelinkAsDefaultBrowser() => Registrar.SetRelinkAsDefaultBrowser();

        internal static bool IsRelinkTheDefaultBrowser => Registrar.IsRelinkTheDefaultBrowser();
    }
}