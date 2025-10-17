using System.Diagnostics;
using ArchivistClient;
using Logging;

namespace EphemeralClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Ephemeral client");
            var log = new ConsoleLog();

            log.Log("Starting...");
            Process.Start("docker", "compose up -d");

            Thread.Sleep(TimeSpan.FromSeconds(30));

            var factory = new ArchivistNodeFactory(log, "datadir");
            var instance = ArchivistInstance.CreateFromApiEndpoint(
                "name",
                new Utils.Address("name", "http://localhost", 8089)
            );
            var node = factory.CreateArchivistNode(instance);

            log.Log(node.GetDebugInfo().ToString());

            log.Log("Stopping...");
            Process.Start("docker", "compose down");
        }
    }
}
