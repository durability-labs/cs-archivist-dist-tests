using ArchivistNetworkConfig;
using FileUtils;
using Logging;
using Utils;

namespace EphemeralClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
        }

        private readonly TimeSpan loopDelay = TimeSpan.FromMinutes(30);
        private readonly ByteSize fileSize = 100.MB();

        private readonly ILog log;
        private readonly LocalNode localNode;
        private readonly GatewayClient.GatewayClient gateway;
        private readonly IFileManager fileManager;
        private readonly string downloadFolder = "temp_downloadfiles";

        public Program()
        {
            log = new TimestampPrefixer(
                new LogSplitter(
                    new FileLog(Path.Combine("logs", "autoclient")),
                    new ConsoleLog()
                )
            );

            Log("Ephemeral client");

            localNode = new LocalNode(log);

            Log("Loading network config...");
            var networkConnector = new ArchivistNetworkConnector(log, "devnet", "latest");
            var network = networkConnector.GetConfig();
            Log($"Network: {network.Name}");
            gateway = new GatewayClient.GatewayClient(network);

            Log("Preparing local node...");
            localNode.Initialize(network);

            fileManager = new FileManager(log, "temp_uploadfiles");
        }

        private void Run()
        {
            Log("Run: start");

            Sleep(10);

            while (true)
            {
                RunCheck();

                Sleep(loopDelay);
            }
        }

        private void RunCheck()
        {
            try
            {
                RunWithNode();
                Log("Check: Success");
            }
            catch (Exception ex)
            {
                log.Error($"Check: Exception: {ex}");
            }
        }

        private void RunWithNode()
        {
            var node = localNode.Start();
            Directory.CreateDirectory(downloadFolder);

            try
            {
                fileManager.ScopedFiles(() =>
                {
                    RunCheckSteps(node);
                });
            }
            finally
            {
                Directory.Delete(downloadFolder, true);
                localNode.StopAndClean();
            }
        }

        private void RunCheckSteps(ArchivistClient.IArchivistNode node)
        {
            var file = fileManager.GenerateFile(fileSize);

            var cid = new ArchivistClient.ContentId();
            var uploadTime = Stopwatch.Measure(log, nameof(node.UploadFile), () =>
            {
                cid = node.UploadFile(file);
            });

            var filename = Path.Combine(downloadFolder, Guid.NewGuid().ToString());
            var downloadTime = Stopwatch.Measure(log, nameof(gateway.Download), () =>
            {
                using var fileStream = File.OpenWrite(filename);
                {
                    var stream = gateway.Download(cid.Id);
                    stream.CopyTo(fileStream);
                }
            });

            var downloaded = TrackedFile.FromPath(log, filename);
            try
            {
                file.AssertIsEqual(downloaded);
            }
            catch (Exception ex)
            {
                log.Error($"Downloaded file was not equal to uploaded file: {ex}");
            }
        }

        private static void Sleep(TimeSpan span)
        {
            Thread.Sleep(span);
        }

        private static void Sleep(int sec)
        {
            Sleep(TimeSpan.FromSeconds(sec));
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
