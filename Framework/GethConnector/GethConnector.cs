using BlockchainUtils;
using ArchivistContractsPlugin;
using ArchivistContractsPlugin.Marketplace;
using GethPlugin;
using Logging;

namespace GethConnector
{
    public class GethConnector
    {
        public IGethNode GethNode { get; }
        public IArchivistContracts ArchivistContracts { get; }

        public static GethConnector? Initialize(ILog log)
        {
            return Initialize(log, new BlockCache(log), new NullRequestsCache());
        }

        public static GethConnector? Initialize(ILog log, BlockCache blockCache, IRequestsCache requestsCache)
        {
            if (!string.IsNullOrEmpty(GethInput.LoadError))
            {
                var msg = "Geth input incorrect: " + GethInput.LoadError;
                log.Error(msg);
                return null;
            }

            var gethNode = new CustomGethNode(log, blockCache, GethInput.GethHost, GethInput.GethPort, GethInput.PrivateKey);

            var config = GetArchivistMarketplaceConfig(gethNode, GethInput.MarketplaceAddress);

            var contractsDeployment = new ArchivistContractsDeployment(
                config: config,
                marketplaceAddress: GethInput.MarketplaceAddress,
                abi: GethInput.ABI,
                tokenAddress: GethInput.TokenAddress
            );

            var contracts = new ArchivistContractsAccess(log, gethNode, contractsDeployment, requestsCache);

            return new GethConnector(gethNode, contracts);
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
