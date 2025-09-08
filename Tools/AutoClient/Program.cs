using ArgsUniform;
using AutoClient;
using AutoClient.Modes;
using ArchivistClient;
using GethPlugin;
using Utils;
using WebUtils;
using Logging;

public class Program
{
    private readonly App app;

    public Program(Configuration config)
    {
        app = new App(config);
    }

    public static void Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, args) => cts.Cancel();

        var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
        var config = uniformArgs.Parse(true);

        if (config.NumConcurrentPurchases < 1)
        {
            throw new Exception("Number of concurrent purchases must be > 0");
        }

        var p = new Program(config);
        p.Run();
    }

    public void Run()
    {
        if (app.Config.ContractDurationMinutes - 1 < 5) throw new Exception("Contract duration config option not long enough!");
        var archivistNodes = CreateArchivistWrappers();
        var loadBalancer = new LoadBalancer(app, archivistNodes);
        loadBalancer.Start();

        var folderStore = new FolderStoreMode(app, loadBalancer);
        folderStore.Start();

        app.Cts.Token.WaitHandle.WaitOne();

        folderStore.Stop();
        loadBalancer.Stop();

        app.Log.Log("Done");
    }

    private ArchivistWrapper[] CreateArchivistWrappers()
    {
        var endpointStrs = app.Config.ArchivistEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<ArchivistWrapper>();

        var i = 1;
        foreach (var e in endpointStrs)
        {
            result.Add(CreateArchivistWrapper(e, i));
            i++;
        }

        return result.ToArray();
    }

    private readonly string LogLevel = "TRACE;info:discv5,providers,routingtable,manager,cache;warn:libp2p,multistream,switch,transport,tcptransport,semaphore,asyncstreamwrapper,lpstream,mplex,mplexchannel,noise,bufferstream,mplexcoder,secure,chronosstream,connection,websock,ws-session,muxedupgrade,upgrade,identify,contracts,clock,serde,json,serialization,JSONRPC-WS-CLIENT,JSONRPC-HTTP-CLIENT,repostore";

    private ArchivistWrapper CreateArchivistWrapper(string endpoint, int number)
    {
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

        var address = new Address(
            logName: $"cdx@{host}:{port}",
            host: host,
            port: port
        );

        var numberStr = number.ToString().PadLeft(3, '0');
        var log = new LogPrefixer(app.Log, $"[{numberStr}] ");
        var httpFactory = new HttpFactory(log, new AutoClientWebTimeSet());
        var archivistNodeFactory = new ArchivistNodeFactory(log: log, httpFactory: httpFactory, dataDir: app.Config.DataPath);
        var instance = ArchivistInstance.CreateFromApiEndpoint($"[AC-{numberStr}]", address, EthAccountGenerator.GenerateNew());
        var node = archivistNodeFactory.CreateArchivistNode(instance);

        node.SetLogLevel(LogLevel);
        return new ArchivistWrapper(app, node);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Archivist storage contracts for it.");
    }
}
