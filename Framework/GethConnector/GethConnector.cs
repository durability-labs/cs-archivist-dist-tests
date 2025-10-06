using BlockchainUtils;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using ArchivistNetworkConfig;
using Utils;

namespace GethConnector
{
    public class GethConnector
    {
        public IGethNode GethNode { get; }
        public IArchivistContracts ArchivistContracts { get; }

        private const string GethPrivKeyVar = "GETH_PRIVATE_KEY";

        public static GethConnector? Initialize(ILog log)
        {
            return Initialize(log, new BlockCache(log), new NullRequestsCache());
        }

        public static GethConnector? Initialize(ILog log, BlockCache blockCache, IRequestsCache requestsCache)
        {
            var privateKey = EnvVar.GetOrThrow(GethPrivKeyVar);
            var networkConfig = FetchNetworkConfig(log);

            var gethNode = new CustomGethNode(log, blockCache, networkConfig.RPCs.First(), privateKey);

            var config = GetArchivistMarketplaceConfig(gethNode, networkConfig.Marketplace.ContractAddress);

            var contractsDeployment = new ArchivistContractsDeployment(
                config: config,
                marketplaceAddress: networkConfig.Marketplace.ContractAddress,
                abi: networkConfig.Marketplace.ABI
            );

            var contracts = new ArchivistContractsAccess(log, gethNode, contractsDeployment, requestsCache);

            return new GethConnector(gethNode, contracts);
        }

        private static ArchivistNetwork FetchNetworkConfig(ILog log)
        {
            try
            {
                var networkConnector = new ArchivistNetworkConnector(log);
                return networkConnector.GetConfig();
            }
            catch (Exception ex)
            {
                log.Error($"Unable to load ArchivistNetworkConfig: " + ex);
                throw;
            }
        }

        private static MarketplaceConfig GetArchivistMarketplaceConfig(IGethNode gethNode, string marketplaceAddress)
        {
            var func = new ConfigurationFunctionBase();
            var response = gethNode.Call<ConfigurationFunctionBase, ConfigurationOutputDTO>(marketplaceAddress, func);
            return response.ReturnValue1;
        }

        private GethConnector(IGethNode gethNode, IArchivistContracts archivistContracts)
        {
            GethNode = gethNode;
            ArchivistContracts = archivistContracts;
        }
    }
}
