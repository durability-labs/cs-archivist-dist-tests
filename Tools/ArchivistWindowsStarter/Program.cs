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
        private const string ArchivistVersion = "v0.0.2";

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

        private void Run()
        {
            Log("Initializing...");

            var connector = new ArchivistNetworkConnector(log, version: ArchivistVersion);
            var config = connector.GetConfig();

            Log($"Archivist network: {config.Name}");
            Log($"Archivist version: {ArchivistVersion}");

            var publicIp = GetPublicIP();
            Log($"Public IP: {publicIp}");

            var args = new List<string>{
                "--data-dir=datadir",
                "--api-cors-origin=*",
                $"--nat=extip:{publicIp}",
                "--log-file=archivist.log",
                "--disc-port=8090",
                "--listen-addrs=/ip4/0.0.0.0/tcp/8070"
            };

            foreach (var spr in config.SPR.Records)
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
                Log("Failed to start node...");
                Log("Error information:");
                var stdout = nodeProcess.StandardOutput.ReadToEnd();
                var stderr = nodeProcess.StandardError.ReadToEnd();
                Log(stdout);
                Log(stderr);
                return;
            }

            Log("Starting WebUI...");
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "https://app.archivist.storage"
            });

            while (true)
            {
                Thread.Sleep(TimeSpan.FromHours(24));
            }
        }

        private string GetPublicIP()
        {
            using var httpClient = new HttpClient();
            var responseTask = httpClient.GetAsync("https://ip.archivist.storage");
            responseTask.Wait();
            var stringTask = responseTask.Result.Content.ReadAsStringAsync();
            stringTask.Wait();
            return stringTask.Result.Trim();
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
