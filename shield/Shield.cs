using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Reflection;

[assembly: AssemblyTitle("Zenith Shield Guard")]
[assembly: AssemblyDescription("Zenith Background Self-Control Guard")]
[assembly: AssemblyCompany("yurtzy")]
[assembly: AssemblyProduct("Zenith Focus Suite")]
[assembly: AssemblyCopyright("Copyright © 2026 yurtzy")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace ZenithShield
{
    class Shield
    {
        private const string ExtensionId = "bonebkgnmbaongbgjfalllepkbkahhda";

        // Trigger words to monitor in window titles
        private static readonly string[] Triggers = new string[]
        {
            "porn", "pornography", "xxx", "hentai", "ecchi", "erotic", "nsfw",
            "milf", "xvideos", "xnxx", "xhamster", "pornhub", "masturbation",
            "masturbate", "orgasm", "blowjob", "cuckold", "gangbang", "camgirl",
            "eroge", "rule34", "doujinshi", "nhentai", "playboy", "nekopoi",
            "hentaihaven", "javfree", "bukkake", "civitai", "comfyui", 
            "stable diffusion", "sillytavern", "bokep", "sange", "ngocok", 
            "lendir", "cersex", "colay", "colok", "seks", "mesum", "porno", "semi"
        };

        [STAThread]
        static void Main(string[] args)
        {
            // Determine execution mode (Main vs Watchdog)
            if (args.Length >= 2 && args[0] == "--watchdog")
            {
                // WATCHDOG MODE: Monitors the main process and resurrects it if killed
                int mainPid;
                if (int.TryParse(args[1], out mainPid))
                {
                    RunWatchdog(mainPid);
                }
                return;
            }

            // Check if Watchdog has been opted-out by the user via setup checkbox configuration
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = Path.GetDirectoryName(currentExe);
            string disableFilePath = Path.Combine(currentDir, "watchdog.disabled");

            if (File.Exists(disableFilePath))
            {
                // Watchdog is disabled! Run as a single, normal process (easily ended in Task Manager)
                RunMainSingle();
            }
            else
            {
                // Watchdog is enabled! Run the mutual keep-alive resurrection loop
                RunMainWithWatchdog(currentExe);
            }
        }

        private static void RunMainSingle()
        {
            // Prevent multiple main instances running concurrently
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ZenithShieldMainMutex", out createdNew))
            {
                if (!createdNew)
                {
                    return; // Already running
                }

                while (true)
                {
                    try
                    {
                        MonitorProcesses();
                    }
                    catch
                    {
                        // Safe catch
                    }
                    Thread.Sleep(1500); // Relaxed check interval when single
                }
            }
        }

        private static void RunMainWithWatchdog(string currentExe)
        {
            // Prevent multiple main instances running concurrently
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ZenithShieldMainMutex", out createdNew))
            {
                if (!createdNew)
                {
                    return; // Main is already running
                }

                Process watchdogProcess = null;

                while (true)
                {
                    try
                    {
                        // Maintain Watchdog process (Resurrection loop)
                        if (watchdogProcess == null || HasProcessExited(watchdogProcess))
                        {
                            int currentPid = Process.GetCurrentProcess().Id;
                            watchdogProcess = Process.Start(new ProcessStartInfo
                            {
                                FileName = currentExe,
                                Arguments = "--watchdog " + currentPid,
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                WindowStyle = ProcessWindowStyle.Hidden
                            });
                        }

                        // Perform system-wide content/process monitoring
                        MonitorProcesses();
                    }
                    catch
                    {
                        // Safe catch
                    }

                    Thread.Sleep(1000); // Check every 1.0 seconds
                }
            }
        }

        private static void RunWatchdog(int mainPid)
        {
            // Prevent multiple watchdog instances running concurrently
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ZenithShieldWatchdogMutex", out createdNew))
            {
                if (!createdNew)
                {
                    return; // Watchdog already running
                }

                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string currentDir = Path.GetDirectoryName(currentExe);

                // Watchdog loop (rapid check to instantly resurrect main if Task Manager kills it)
                while (true)
                {
                    try
                    {
                        // Check if the main process is still active
                        Process mainProcess = Process.GetProcessById(mainPid);
                        if (mainProcess == null || mainProcess.HasExited)
                        {
                            ResurrectMain(currentExe, currentDir);
                            return; 
                        }
                    }
                    catch (ArgumentException)
                    {
                        ResurrectMain(currentExe, currentDir);
                        return;
                    }
                    catch
                    {
                        // Safe ignore
                    }

                    Thread.Sleep(300); // 300ms heartbeat
                }
            }
        }

        private static void ResurrectMain(string currentExe, string currentDir)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentExe,
                    WorkingDirectory = currentDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch {}
        }

        private static bool HasProcessExited(Process proc)
        {
            try
            {
                return proc.HasExited;
            }
            catch
            {
                return true;
            }
        }

        private static void MonitorProcesses()
        {
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                try
                {
                    string processName = process.ProcessName.ToLower();

                    // Skip system, self, and all known browsers (browsers are handled by the Chrome extension)
                    if (processName == "idle" || processName == "system" ||
                        processName == "zenith-shield" || processName == "chrome" ||
                        processName == "msedge" || processName == "firefox" ||
                        processName == "opera" || processName == "brave" ||
                        processName == "iexplore" || processName == "waterfox" ||
                        processName == "vivaldi" || processName == "arc" ||
                        processName == "thorium" || processName == "librewolf" ||
                        processName == "yandex")
                    {
                        continue;
                    }

                    string windowTitle = process.MainWindowTitle.ToLower();

                    if (string.IsNullOrEmpty(windowTitle))
                    {
                        continue;
                    }

                    foreach (string trigger in Triggers)
                    {
                        if (IsExplicitMatch(windowTitle, trigger))
                        {
                            process.Kill();
                            LaunchZenithIntervention(trigger, processName);
                            break;
                        }
                    }
                }
                catch
                {
                    // Process might have exited or permission denied
                }
            }
        }

        private static bool IsExplicitMatch(string title, string trigger)
        {
            if (title.Contains(trigger))
            {
                int index = title.IndexOf(trigger);
                
                if (index > 0 && char.IsLetterOrDigit(title[index - 1]))
                {
                    return false;
                }

                int afterIndex = index + trigger.Length;
                if (afterIndex < title.Length && char.IsLetterOrDigit(title[afterIndex]))
                {
                    return false;
                }

                return true;
            }
            return false;
        }

        private static void LaunchZenithIntervention(string trigger, string sourceApp)
        {
            try
            {
                string url = string.Format("chrome-extension://{0}/intervention/intervention.html?trigger={1}&original={2}", 
                    ExtensionId, 
                    Uri.EscapeDataString("system-bypass (" + trigger + ")"), 
                    Uri.EscapeDataString(sourceApp)
                );

                Process.Start("chrome.exe", "--new-window \"" + url + "\"");
            }
            catch
            {
                try
                {
                    Process.Start("chrome-extension://" + ExtensionId + "/intervention/intervention.html");
                }
                catch {}
            }
        }
    }
}
