using BlockchainUtils;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using Core;
using GethPlugin;
using Logging;
using Utils;

namespace TraceContract
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<ArchivistContractsPlugin.ArchivistContractsPlugin>();

            var p = new Program();
            p.Run();
        }

        private readonly ILog baseLog;
        private readonly ILog appLog;
        private readonly Input input = new();
        private readonly Config config = new();
        private readonly Output output;

        public Program()
        {
            baseLog = new TimestampPrefixer(
                new LogSplitter(
                    new ConsoleLog(),
                    new FileLog(Path.Combine(config.DataDir, "logs"))
                )
            );

            appLog = new LogPrefixer(baseLog, "(TraceContract)");
            output = new(appLog, input, config);
        }

        private void Run()
        {
            try
            {
                TracePurchase();
            }
            catch (Exception exc)
            {
                appLog.Error(exc.ToString());
            }
        }

        private void TracePurchase()
        { 
            Log("Setting up...");
            var entryPoint = new EntryPoint(baseLog, new KubernetesWorkflow.Configuration(null, TimeSpan.FromMinutes(1.0), TimeSpan.FromSeconds(10.0), "_Unused!_"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            entryPoint.Announce();
            var ci = entryPoint.CreateInterface();
            var geth = ConnectGethNode();
            var contracts = ConnectArchivistContracts(ci, geth);

            var chainTracer = new ChainTracer(appLog, baseLog, geth, contracts, input, output);
            var requestTimeRange = chainTracer.TraceChainTimeline();

            Log("Downloading storage nodes logs for the request timerange...");
            DownloadStorageNodeLogs(requestTimeRange, entryPoint.Tools);

            output.ShowOutputFiles(appLog);

            entryPoint.Decommission(false, false, false);
            Log("Done");
        }

        private IGethNode ConnectGethNode()
        {
            var account = EthAccountGenerator.GenerateNew();
            var blockCache = new BlockCache(baseLog, new DiskBlockBucketStore(Path.Combine(config.DataDir, "blocks_cache")));
            return new CustomGethNode(baseLog, blockCache, $"{config.RpcEndpoint}:{config.GethPort}", account.PrivateKey);
        }

        private IArchivistContracts ConnectArchivistContracts(CoreInterface ci, IGethNode geth)
        {
            var deployment = new ArchivistContractsDeployment(
                config: new MarketplaceConfig(),
                marketplaceAddress: config.MarketplaceAddress,
                abi: config.Abi
            );
            return ci.WrapArchivistContractsDeployment(geth, deployment, new DiskRequestsCache(Path.Combine(config.DataDir, "requests_cache")));
        }

        private void DownloadStorageNodeLogs(TimeRange requestTimeRange, IPluginTools tools)
        {
            var start = requestTimeRange.From - config.LogStartBeforeStorageContractStarts;

            foreach (var node in config.StorageNodesKubernetesPodNames)
            {
                Log($"Downloading logs from '{node}'...");

                var targetFile = output.CreateNodeLogTargetFile(node);
                var downloader = new ElasticSearchLogDownloader(baseLog, tools, config);
                downloader.Download(targetFile, node, start, requestTimeRange.To);
            }
        }

        private void Log(string msg)
        {
            appLog.Log(msg);
        }
    }
}
