using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistPlugin;
using Core;
using GethPlugin;
using Utils;

namespace ArchivistNetDeployer
{
    public class ArchivistNodeStarter
    {
        private readonly Configuration config;
        private readonly CoreInterface ci;
        private readonly IGethNode gethNode;
        private readonly IArchivistContracts contracts;
        private IArchivistNode? bootstrapNode = null;
        private int validatorsLeft;

        public ArchivistNodeStarter(Configuration config, CoreInterface ci, IGethNode gethNode, IArchivistContracts contracts, int numberOfValidators)
        {
            this.config = config;
            this.ci = ci;
            this.gethNode = gethNode;
            this.contracts = contracts;
            validatorsLeft = numberOfValidators;
        }

        public ArchivistNodeStartResult? Start(int i)
        {
            var name = GetArchivistContainerName(i);
            Console.Write($" - {i} ({name})\t");
            Console.CursorLeft = 30;

            IArchivistNode? archivistNode = null;
            try
            {
                archivistNode = ci.StartArchivistNode(s =>
                {
                    s.WithName(name);
                    s.WithLogLevel(config.ArchivistLogLevel, new ArchivistLogCustomTopics(config.Discv5LogLevel, config.Libp2pLogLevel));
                    s.WithStorageQuota(config.StorageQuota!.Value.MB());

                    if (config.ShouldMakeStorageAvailable)
                    {
                        s.EnableMarketplace(gethNode, contracts, m =>
                        {
                            m.WithInitial(100.Eth(), config.InitialTestTokens.TstWei());
                            if (validatorsLeft > 0) m.AsValidator();
                            if (config.ShouldMakeStorageAvailable) m.AsStorageNode();
                        });
                    }

                    if (bootstrapNode != null) s.WithBootstrapNode(bootstrapNode);
                    if (config.MetricsEndpoints) s.EnableMetrics();
                    if (config.BlockTTL != Configuration.SecondsIn1Day) s.WithBlockTTL(TimeSpan.FromSeconds(config.BlockTTL));
                    if (config.BlockMI != Configuration.TenMinutes) s.WithBlockMaintenanceInterval(TimeSpan.FromSeconds(config.BlockMI));
                    if (config.BlockMN != 1000) s.WithBlockMaintenanceNumber(config.BlockMN);

                    if (config.IsPublicTestNet)
                    {
                        s.AsPublicTestNet(CreatePublicTestNetConfig(i));
                    }
                });
            
                var debugInfo = archivistNode.GetDebugInfo();
                if (!string.IsNullOrWhiteSpace(debugInfo.Spr))
                {
                    Console.Write("Online\t");

                    if (config.ShouldMakeStorageAvailable)
                    {
                        var availability = new CreateStorageAvailability(
                            totalSpace: config.StorageSell!.Value.MB(),
                            maxDuration: TimeSpan.FromSeconds(config.MaxDuration),
                            minPricePerBytePerSecond: config.MinPricePerBytePerSecond.TstWei(),
                            totalCollateral: config.MaxCollateral.TstWei()
                        );

                        var response = archivistNode.Marketplace.MakeStorageAvailable(availability);

                        if (!string.IsNullOrEmpty(response))
                        {
                            Console.Write("Storage available\t");
                        }
                        else throw new Exception("Failed to make storage available.");
                    }
                    
                    Console.Write("OK" + Environment.NewLine);

                    validatorsLeft--;
                    if (bootstrapNode == null) bootstrapNode = archivistNode;
                    return new ArchivistNodeStartResult(archivistNode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.ToString());
            }

            Console.WriteLine("Unknown failure.");
            if (archivistNode != null)
            {
                Console.WriteLine("Downloading container log.");
                archivistNode.DownloadLog();
            }

            return null;
        }

        private ArchivistTestNetConfig CreatePublicTestNetConfig(int i)
        {
            var discPort = config.PublicDiscPorts.Split(",")[i];
            var listenPort = config.PublicListenPorts.Split(",")[i];

            return new ArchivistTestNetConfig
            {
                PublicDiscoveryPort = Convert.ToInt32(discPort),
                PublicListenPort = Convert.ToInt32(listenPort)
            };
        }

        private string GetArchivistContainerName(int i)
        {
            if (i == 0) return "BOOTSTRAP";
            return "ARCHIVIST" + i;
        }
    }

    public class ArchivistNodeStartResult
    {
        public ArchivistNodeStartResult(IArchivistNode archivistNode)
        {
            ArchivistNode = archivistNode;
        }

        public IArchivistNode ArchivistNode { get; }
    }
}
