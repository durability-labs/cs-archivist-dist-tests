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
                $"--nat=extip:{publicIp}",
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

            Log("Starting WebUI...");
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "https://app.archivist.storage"
            });

            Thread.Sleep(TimeSpan.MaxValue);
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
