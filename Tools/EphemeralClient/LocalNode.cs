using System.Diagnostics;
using ArchivistClient;
using ArchivistNetworkConfig;
using Logging;

namespace EphemeralClient
{
    public class LocalNode
    {
        private readonly ILog log;

        public LocalNode(ILog log)
        {
            this.log = new LogPrefixer(log, "(LocalNode)");
        }

        public void Initialize(ArchivistNetwork network)
        {
            var filename = "docker-compose.yaml";
            if (!File.Exists(filename))
            {
                log.Error($"File '{filename}' does not exist.");
                throw new FileNotFoundException(filename);
            }

            var targetLine = "      - NETWORK=";
            var lines = File.ReadAllLines(filename);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(targetLine))
                {
                    // In-place update and done.
                    lines[i] = targetLine + network.Name;
                    File.WriteAllLines(filename, lines);
                    return;
                }
            }

            log.Error($"Did not find target line '{targetLine}' in file '{filename}'");
            throw new InvalidDataException();
        }

        public IArchivistNode Start()
        {
            log.Log("Starting...");
            Process.Start("docker", "compose up -d");

            Thread.Sleep(TimeSpan.FromSeconds(20.0));

            var factory = new ArchivistNodeFactory(log, "datadir");
            var instance = ArchivistInstance.CreateFromApiEndpoint(
                "name",
                new Utils.Address("name", "http://localhost", 8089)
            );
            return factory.CreateArchivistNode(instance);
        }

        public void StopAndClean()
        {
            log.Log("Stopping...");
            Process.Start("docker", "compose down");

            Thread.Sleep(TimeSpan.FromSeconds(10.0));

            log.Log("Cleaning up...");
            Directory.Delete("volumes", true);
        }
    }
}
