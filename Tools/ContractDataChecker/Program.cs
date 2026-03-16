using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistNetworkConfig;
using ArgsUniform;
using BlockchainUtils;
using ContractDataChecker;
using Logging;
using TestNetRewarder;

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

        var log = new LogPrefixer(new LogSplitter(
             new ConsoleLog(),
             new FileLog(Path.Combine(config.LogPath, "cdt"))
         ), "(CDT)");

        var netConnector = new ArchivistNetworkConnector(log);
        var network = netConnector.GetConfig();

        var diskStore = new DiskBlockBucketStore(log, Path.Join(config.DataPath, "blockcache"));
        var blockCache = new BlockCache(log, diskStore);
        var requestsCache = new DiskRequestsCache(Path.Join(config.DataPath, "requestscache"));
        var rpcConnector = GethConnector.GethConnector.Initialize(log, network, blockCache, requestsCache);
        if (rpcConnector == null) throw new Exception("Invalid Eth RPC information");

        var factory = new ArchivistNodeFactory(log, "datadir");
        var addrsTokens = config.ArchivistEndpoint.Split(":");
        var instance = ArchivistInstance.CreateFromApiEndpoint(
            "cdt",
            new Utils.Address("cdt", addrsTokens[0], Convert.ToInt32(addrsTokens[1]))
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
            new DataChecker(log, archivistNode),
            new DoNothingChainEventHandler(),
            null
        ));
    }

    private async Task Run()
    {
        await chainFollower.Run(ct);
    }
}
