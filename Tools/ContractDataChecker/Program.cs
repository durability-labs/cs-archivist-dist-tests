using ArchivistContractsPlugin;
using ArchivistNetworkConfig;
using ArgsUniform;
using BlockchainUtils;
using ContractDataChecker;
using GethPlugin;
using Logging;

public class Program
{
    public static void Main(string[] args)
    {
        var uniformArgs = new ArgsUniform<Configuration>(args);
        var config = uniformArgs.Parse(true);

        var p = new Program(config);
        p.Run();
    }

    private readonly ILog log;
    private readonly Configuration config;
    private readonly ArchivistNetwork network;
    private readonly IGethNode rpcNode;

    public Program(Configuration config)
    {
        this.config = config;
        log = new LogPrefixer(new LogSplitter(
             new ConsoleLog(),
             new FileLog(Path.Combine(config.LogPath, "cdt"))
         ), "(CDT)");

        var netConnector = new ArchivistNetworkConnector(log);
        network = netConnector.GetConfig();

        var diskStore = new DiskBlockBucketStore(log, Path.Join(config.DataPath, "blockcache"));
        var blockCache = new BlockCache(log, diskStore);
        var requestsCache = new DiskRequestsCache(Path.Join(config.DataPath, "requestscache"));
        var rpcConnector = GethConnector.GethConnector.Initialize(log, network, blockCache, requestsCache);
        if (rpcConnector == null) throw new Exception("Invalid Eth RPC information");
        rpcNode = rpcConnector.GethNode;
    }

    private void Run()
    {
        log.Log("Contract Data Tester...");


    }
}
