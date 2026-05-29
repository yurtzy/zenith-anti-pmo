using System;
using System.Diagnostics;
using System.Reflection;

[assembly: AssemblyTitle("Zenith App Launcher")]
[assembly: AssemblyDescription("Zenith Overview Dashboard App Shell")]
[assembly: AssemblyCompany("yurtzy")]
[assembly: AssemblyProduct("Zenith Focus Suite")]
[assembly: AssemblyCopyright("Copyright © 2026 yurtzy")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace ZenithAppLauncher
{
    class App
    {
        // Stable Chrome Extension ID
        private const string ExtensionId = "bonebkgnmbaongbgjfalllepkbkahhda";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Format the app mode Chrome launch argument
                // --app=URL launches Chrome in standalone, borderless, app-shell window mode (no URL bar, no tabs)
                string url = string.Format("chrome-extension://{0}/dashboard/dashboard.html", ExtensionId);
                string arguments = string.Format("--app=\"{0}\"", url);

                // Start Chrome in app mode
                Process.Start("chrome.exe", arguments);
            }
            catch
            {
                // Fallback to launching in standard default browser if Chrome call fails
                try
                {
                    Process.Start("chrome-extension://" + ExtensionId + "/dashboard/dashboard.html");
                }
                catch {}
            }
        }
    }
}
