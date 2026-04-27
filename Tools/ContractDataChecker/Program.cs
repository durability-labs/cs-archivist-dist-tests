using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistNetworkConfig;
using ArgsUniform;
using BlockchainUtils;
using ChainFollowingApp;
using ContractDataChecker;
using Logging;

public class Program
{
    public static Task Main(string[] args)
    {
        var uniformArgs = new ArgsUniform<Configuration>(args);
        var config = uniformArgs.Parse(true);

        var p = new Program(config);
        return p.Run();
    }

    private readonly ChainFollowing chainFollower;
    private readonly CancellationToken ct;

    public Program(Configuration config)
    {
        var cts = new CancellationTokenSource();
        ct = cts.Token;
        Console.CancelKeyPress += (sender, args) => cts.Cancel();

        var log = new LogPrefixer(new TimestampPrefixer(new LogSplitter(
             new ConsoleLog(),
             new FileLog(Path.Combine(config.LogPath, "cdt"))
         )), "(CDT)");

        log.Log("  --  [[ Contract Data Tester ]]  --");
        log.Log("Getting Archivist network configuration...");
        var netConnector = new ArchivistNetworkConnector(log);
        var network = netConnector.GetConfig();

        log.Log("Initializing RPC connector...");
        var diskStore = new DiskBlockBucketStore(log, Path.Join(config.DataPath, "blockcache"));
        var blockCache = new BlockCache(log, diskStore);
        var requestsCache = new DiskRequestsCache(Path.Join(config.DataPath, "requestscache"));
        var rpcConnector = GethConnector.GethConnector.Initialize(log, network, blockCache, requestsCache);
        if (rpcConnector == null) throw new Exception("Invalid Eth RPC information");

        log.Log("Creating Archivist client instance...");
        var factory = new ArchivistNodeFactory(log, "datadir");
        var endpoint = config.ArchivistEndpoint;
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));
        var instance = ArchivistInstance.CreateFromApiEndpoint(
            "node",
            new Utils.Address("node", host, port)
        );
        var archivistNode = factory.CreateArchivistNode(instance);

        chainFollower = new ChainFollowing(new ChainFollowConfig(
            log,
            config.Interval,
            config.HistoryStartUtc,
            rpcConnector.GethNode,
            rpcConnector.ArchivistContracts,
            requestsCache
        ), new ChainFollowHandlers(
            new DataChecker(log, config, archivistNode),
            new DoNothingChainEventHandler(),
            null
        ));
     
        log.Log("Activating chain-follower...");
    }

    private async Task Run()
    {
        await chainFollower.Run(ct);
    }
}
