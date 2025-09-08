using BlockchainUtils;
using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistDiscordBotPlugin;
using ArchivistPlugin;
using Core;
using GethPlugin;
using KubernetesWorkflow.Types;
using Logging;
using MetricsPlugin;
using WebUtils;

namespace ArchivistNetDeployer
{
    public class Deployer
    {
        private readonly Configuration config;
        private readonly PeerConnectivityChecker peerConnectivityChecker;
        private readonly EntryPoint entryPoint;
        private readonly LocalArchivistBuilder localArchivistBuilder;

        public Deployer(Configuration config)
        {
            this.config = config;
            peerConnectivityChecker = new PeerConnectivityChecker();
            localArchivistBuilder = new LocalArchivistBuilder(new ConsoleLog(), config.ArchivistLocalRepoPath);

            ProjectPlugin.Load<ArchivistPlugin.ArchivistPlugin>();
            ProjectPlugin.Load<ArchivistContractsPlugin.ArchivistContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
            ProjectPlugin.Load<ArchivistDiscordBotPlugin.ArchivistDiscordBotPlugin>();
            entryPoint = CreateEntryPoint(new NullLog());
        }

        public void AnnouncePlugins()
        {
            var ep = CreateEntryPoint(new ConsoleLog());

            localArchivistBuilder.Intialize();

            Log("Using plugins:" + Environment.NewLine);
            var metadata = ep.GetPluginMetadata();
            var longestKey = metadata.Keys.Max(k => k.Length);
            foreach (var entry in metadata)
            {
                Console.Write(entry.Key);
                Console.CursorLeft = longestKey + 5;
                Console.WriteLine($"= {entry.Value}");
            }

            Log("");
        }

        public ArchivistDeployment Deploy()
        {
            localArchivistBuilder.Build();

            Log("Initializing...");
            var startUtc = DateTime.UtcNow;
            var ci = entryPoint.CreateInterface();

            Log("Deploying Geth instance...");
            var gethDeployment = DeployGeth(ci);
            var gethNode = ci.WrapGethDeployment(gethDeployment, new BlockCache());

            var bootNode = ci.StartArchivistNode();
            var versionInfo = bootNode.GetDebugInfo().Version;
            bootNode.Stop(waitTillStopped: true);

            Log("Geth started. Deploying Archivist contracts...");
            var contractsDeployment = ci.DeployArchivistContracts(gethNode, versionInfo);
            var contracts = ci.WrapArchivistContractsDeployment(gethNode, contractsDeployment);
            Log("Archivist contracts deployed.");

            Log("Starting Archivist nodes...");
            var archivistStarter = new ArchivistNodeStarter(config, ci, gethNode, contracts, config.NumberOfValidators!.Value);
            var startResults = new List<ArchivistNodeStartResult>();
            for (var i = 0; i < config.NumberOfArchivistNodes; i++)
            {
                var result = archivistStarter.Start(i);
                if (result != null) startResults.Add(result);
            }

            Log("Archivist nodes started.");
            var metricsService = StartMetricsService(ci, startResults);

            CheckPeerConnectivity(startResults);
            CheckContainerRestarts(startResults);

            var archivistInstances = CreateArchivistInstances(startResults);

            var discordBotContainer = DeployDiscordBot(ci, gethDeployment, contractsDeployment);

            return new ArchivistDeployment(archivistInstances, gethDeployment, contractsDeployment, metricsService,
                discordBotContainer, CreateMetadata(startUtc), config.DeployId);
        }

        private EntryPoint CreateEntryPoint(ILog log)
        {
            var kubeConfig = GetKubeConfig(config.KubeConfigFile);

            var configuration = new KubernetesWorkflow.Configuration(
                kubeConfig,
                operationTimeout: TimeSpan.FromMinutes(10),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: config.KubeNamespace);

            var result = new EntryPoint(log, configuration, string.Empty, new FastHttpTimeSet(), new DefaultK8sTimeSet());
            configuration.Hooks = new K8sHook(config.TestsTypePodLabel, config.DeployId, result.GetPluginMetadata());

            return result;
        }

        private GethDeployment DeployGeth(CoreInterface ci)
        {
            return ci.DeployGeth(s =>
            {
                s.IsMiner();
                s.WithName("geth");

                if (config.IsPublicTestNet)
                {
                    s.AsPublicTestNet(new GethTestNetConfig(
                        discoveryPort: config.PublicGethDiscPort,
                        listenPort: config.PublicGethListenPort
                    ));
                }
            });
        }

        private RunningPod? DeployDiscordBot(CoreInterface ci, GethDeployment gethDeployment,
            ArchivistContractsDeployment contractsDeployment)
        {
            if (!config.DeployDiscordBot) return null;
            Log("Deploying Discord bot...");

            var addr = gethDeployment.Container.GetInternalAddress(GethContainerRecipe.HttpPortTag);
            var info = new DiscordBotGethInfo(
                host: addr.Host,
                port: addr.Port,
                privKey: gethDeployment.Account.PrivateKey,
                marketplaceAddress: contractsDeployment.MarketplaceAddress,
                tokenAddress: contractsDeployment.TokenAddress,
                abi: contractsDeployment.Abi
            );

            var rc = ci.DeployArchivistDiscordBot(new DiscordBotStartupConfig(
                name: "discordbot-" + config.DeploymentName,
                token: config.DiscordBotToken,
                serverName: config.DiscordBotServerName,
                adminRoleName: config.DiscordBotAdminRoleName,
                adminChannelName: config.DiscordBotAdminChannelName,
                kubeNamespace: config.KubeNamespace,
                gethInfo: info,
                rewardChannelName: config.DiscordBotRewardChannelName)
            {
                DataPath = config.DiscordBotDataPath
            });

            Log("Discord bot deployed.");
            return rc;
        }

        private RunningPod? StartMetricsService(CoreInterface ci, List<ArchivistNodeStartResult> startResults)
        {
            if (!config.MetricsScraper || !startResults.Any()) return null;

            Log("Starting metrics service...");

            var runningContainer = ci.DeployMetricsCollector(scrapeInterval: TimeSpan.FromSeconds(10.0), startResults.Select(r => r.ArchivistNode).ToArray());

            Log("Metrics service started.");

            return runningContainer;
        }

        private ArchivistInstance[] CreateArchivistInstances(List<ArchivistNodeStartResult> startResults)
        {
            // When freshly started, the Archivist nodes are announcing themselves by an incorrect IP address.
            // Only after fully initialized do they update to the provided NAT address.
            // Therefore, we wait:
            Thread.Sleep(TimeSpan.FromSeconds(5));

            return startResults.Select(r => CreateArchivistInstance(r.ArchivistNode)).ToArray();
        }

        private ArchivistInstance CreateArchivistInstance(IArchivistNode node)
        {
            //return new ArchivistInstance(node.Container.RunningPod, node.GetDebugInfo());
            throw new NotImplementedException();
        }

        private string? GetKubeConfig(string kubeConfigFile)
        {
            if (string.IsNullOrEmpty(kubeConfigFile) || kubeConfigFile.ToLowerInvariant() == "null") return null;
            return kubeConfigFile;
        }

        private void CheckPeerConnectivity(List<ArchivistNodeStartResult> archivistContainers)
        {
            if (!config.CheckPeerConnection || !archivistContainers.Any()) return;

            Log("Starting peer connectivity check for deployed nodes...");
            peerConnectivityChecker.CheckConnectivity(archivistContainers);
            Log("Check passed.");
        }

        private void CheckContainerRestarts(List<ArchivistNodeStartResult> startResults)
        {
            var crashes = new List<IArchivistNode>();
            Log("Starting container crash check...");
            foreach (var startResult in startResults)
            {
                var hasCrashed = startResult.ArchivistNode.HasCrashed();
                if (hasCrashed) crashes.Add(startResult.ArchivistNode);
            }

            if (!crashes.Any())
            {
                Log("Check passed.");
            }
            else
            {
                Log($"Check failed. The following containers have crashed: {crashes.Names()}");
                throw new Exception("Deployment failed: One or more containers crashed.");
            }
        }

        private DeploymentMetadata CreateMetadata(DateTime startUtc)
        {
            return new DeploymentMetadata(
                name: config.DeploymentName,
                startUtc: startUtc,
                finishedUtc: DateTime.UtcNow,
                kubeNamespace: config.KubeNamespace,
                numberOfArchivistNodes: config.NumberOfArchivistNodes!.Value,
                numberOfValidators: config.NumberOfValidators!.Value,
                storageQuotaMB: config.StorageQuota!.Value,
                archivistLogLevel: config.ArchivistLogLevel,
                initialTestTokens: config.InitialTestTokens,
                minPrice: config.MinPricePerBytePerSecond,
                maxCollateral: config.MaxCollateral,
                maxDuration: config.MaxDuration,
                blockTTL: config.BlockTTL,
                blockMI: config.BlockMI,
                blockMN: config.BlockMN);
        }

        private void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }

    public class FastHttpTimeSet : IWebCallTimeSet
    {
        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(2);
        }

        public TimeSpan HttpRetryTimeout()
        {
            return TimeSpan.FromSeconds(30);
        }

        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromSeconds(10);
        }
    }
}
