using ArchivistContractsPlugin;
using ArchivistNetworkConfig;
using BlockchainUtils;
using GethPlugin;
using Logging;

namespace ChainStateAPI.Services
{
    public interface IDeploymentService
    {
        IArchivistContracts Contracts { get; }
        IGethNode RpcNode { get; }
    }

    public class DeploymentService : IDeploymentService
    {
        private readonly ILog log;

        public DeploymentService(ILog log)
        {
            this.log = new LogPrefixer(log, "Deployment");
        }

        public IArchivistContracts Contracts { get; private set; } = null!;
        public IGethNode RpcNode { get; private set; } = null!;

        public void Initialize()
        {
            var connector = new ArchivistNetworkConnector(log);
            var network = connector.GetConfig();

            log.Log($"Network: {network.Name}");
            log.Log($"Archivist version: {network.Version.Version}");
            log.Log($"Archivist revision: {network.Version.Revision}");
            log.Log($"Contracts revision: {network.Version.Contracts}");
            log.Log($"Contracts address: {network.Marketplace.ContractAddress}");

            var blockStore = new DiskBlockBucketStore(log, "blockcache");
            var blockCache = new BlockCache(log, blockStore);
            var requestsCache = new DiskRequestsCache("requestscache");

            var gethConnector = GethConnector.GethConnector.Initialize(log, network, blockCache, requestsCache, EthAccountGenerator.GenerateNew().PrivateKey);
            if (gethConnector == null)
            {
                log.Error("Failed to initialize RPC connector.");
                throw new InvalidOperationException();
            }

            Contracts = gethConnector.ArchivistContracts;
            RpcNode = gethConnector.GethNode;
            log.Log("Initialized");
        }
    }
}
