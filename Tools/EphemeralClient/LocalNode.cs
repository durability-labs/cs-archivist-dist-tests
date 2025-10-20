using System.Diagnostics;
using ArchivistClient;
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
