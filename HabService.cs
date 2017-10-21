using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace HabService
{
    public partial class HabService : ServiceBase
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        private delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // Enumerated type for the control messages sent to the handler routine
        enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private Process proc = null;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("HabService");

        /// <summary>
        /// The main entry point for the service.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HabService()
            };
            Run(ServicesToRun);
        }

        public HabService()
        {
            ServiceName = "HabService";
            CanStop = true;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            ConfigureDebug();
            proc = new Process();
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.FileName = LauncherPath;
            var launcherArgs = "run";
            if(ConfigurationManager.AppSettings["launcherArgs"] != null)
            {
                launcherArgs += $" {ConfigurationManager.AppSettings["launcherArgs"]}";
            }
            proc.StartInfo.Arguments = launcherArgs;
            log.Info($"Habitat windows service is starting launcher with args: {launcherArgs}");
            proc.OutputDataReceived += new DataReceivedEventHandler(SupOutputHandler);
            proc.ErrorDataReceived += new DataReceivedEventHandler(SupOutputHandler);
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
        }

        private static void ConfigureDebug()
        {
            if (ConfigurationManager.AppSettings["debug"] != null)
            {
                if (ConfigurationManager.AppSettings["debug"].ToLower() != "false")
                {
                    Environment.SetEnvironmentVariable("RUST_LOG", "debug");
                }
                else
                {
                    Environment.SetEnvironmentVariable("RUST_LOG", null);
                }
            }
            else
            {
                Environment.SetEnvironmentVariable("RUST_LOG", null);
            }
        }

        private static string LauncherPath
        {
            get
            {
                if (ConfigurationManager.AppSettings["launcherPath"] != null)
                {
                    return ConfigurationManager.AppSettings["launcherPath"];
                }
                else
                {
                    // because we declare hab-launcher as a runtime dep
                    // this path should exist
                    var launcherBase = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "hab\\pkgs\\core\\hab-launcher");
                    var latestLauncher = LastDirectory(LastDirectory(launcherBase));
                    return Path.Combine(latestLauncher, "bin\\hab-launch.exe");
                }
            }
        }

        private static string LastDirectory(string path)
        {
            var dirs = Directory.GetDirectories(path);
            return dirs[dirs.Length - 1];
        }

        private void SupOutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                log.Info(e.Data);
            }
        }

        protected override void OnStop()
        {
            // As a service we have no console so attach to the console of the launcher
            AttachConsole((uint)proc.Id);
            // Turn off our own Ctrl-C handler so we don't die
            SetConsoleCtrlHandler(null, true);
            // Broadcast the ctrl-c
            GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);

            proc.WaitForExit();

            // Remove ourselves from the dead console
            FreeConsole();

            log.Info("Habitat service stopped");
        }
    }
}
