using ArchivistNetworkConfig;
using Logging;
using System.Diagnostics;

namespace ArchivistWindowsStarter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var p = new Program(args);
            p.Run();
        }

        private readonly ILog log;
        private Process? nodeProcess;

        public Program(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            log = new ConsoleLog();
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            try
            {
                if (nodeProcess != null)
                {
                    nodeProcess.Kill();
                }
            }
            catch { }
        }

        private void ShowError(params string[] lines)
        {
            Log("Error:");
            foreach (var l in lines)
            {
                Log($"   {l}");
            }
            Log("");
            // Keep the terminal open.
            Console.ReadLine();
            Console.ReadLine();
        }

        private void Run()
        {
            Log("Initializing...");

            var config = ConfigFile.Load();
            if (config == null)
            {
                ShowError(
                    $"   Failed to load configuration from file '{ConfigFile.Filename}'",
                    "   Make sure the file is correct.",
                    "   You can delete it to reset to defaults.",
                    "   If the problem persist, please report it to Durability-labs."
                );
                return;
            }

            Log($"Archivist network: {config.Network}");
            Log($"Archivist version: {config.Version}");

            Log("Fetching network information...");
            var network = FetchNetworkInfo(config);
            if (network == null)
            {
                ShowError(
                    $"   Failed to fetch network information.",
                    "   Make sure the selected network and version values are correct.",
                    "   You can delete the config file to reset to defaults.",
                    "   If the problem persist, please report it to Durability-labs."
                );
                return;
            }

            var publicIp = GetPublicIP();
            if (publicIp == null)
            {
                ShowError(
                    $"   Failed determine public IP address.",
                    "   Please try again later.",
                    "   If the problem persist, please report it to Durability-labs."
                );
                return;
            }
            Log($"Public IP: {publicIp}");

            var datadir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "archivist", "datadir");
            Log($"Data dir: {datadir}");

            var logfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "archivist", "archivist.log");
            Log($"Log file: {logfile}");

            var args = new List<string>{
                $"--data-dir={datadir}",
                "--api-cors-origin=*",
                $"--nat=extip:{publicIp}",
                $"--log-file={logfile}",
                "--disc-port=8090",
                "--listen-addrs=/ip4/0.0.0.0/tcp/8070"
            };

            foreach (var spr in network.SPR.Records)
            {
                args.Add($"--bootstrap-node={spr}");
            }

            Log("Starting node...");
            var info = new ProcessStartInfo
            {
                FileName = "archivist-v0.0.2-windows-amd64.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var a in args) info.ArgumentList.Add(a);

            nodeProcess = Process.Start(info);

            Thread.Sleep(TimeSpan.FromSeconds(3));
            if (nodeProcess != null && nodeProcess.HasExited)
            {
                var stdout = nodeProcess.StandardOutput.ReadToEnd();
                var stderr = nodeProcess.StandardError.ReadToEnd();

                ShowError(
                    "Failed to start node process.",
                    "Error information:",
                    stdout,
                    stderr,
                    " ",
                    "Please report this to Durability-labs."
                );
                return;
            }

            Log("Starting WebUI...");
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "https://app.archivist.storage"
            });

            Log("Ready");
            Log(" ");
            Log("[ Close this window to stop your Archivist node ]");

            while (true)
            {
                Thread.Sleep(TimeSpan.FromHours(24));
            }
        }

        private ArchivistNetwork? FetchNetworkInfo(ConfigFile config)
        {
            try
            {
                var connector = new ArchivistNetworkConnector(log, network: config.Network, version: config.Version);
                return connector.GetConfig();
            }
            catch
            {
                return null;
            }
        }

        private string? GetPublicIP()
        {
            try
            {
                using var httpClient = new HttpClient();
                var responseTask = httpClient.GetAsync("https://ip.archivist.storage");
                responseTask.Wait();
                var stringTask = responseTask.Result.Content.ReadAsStringAsync();
                stringTask.Wait();
                return stringTask.Result.Trim();
            }
            catch
            {
                return null;
            }
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
