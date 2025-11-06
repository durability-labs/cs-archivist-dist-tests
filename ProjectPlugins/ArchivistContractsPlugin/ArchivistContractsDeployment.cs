using ArchivistContractsPlugin.Marketplace;
using Utils;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractsDeployment
    {
        public ArchivistContractsDeployment(MarketplaceConfig config, ContractAddress marketplaceAddress, string abi)
        {
            Config = config;
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
        }

        public MarketplaceConfig Config { get; }
        public ContractAddress MarketplaceAddress { get; }
        public string Abi { get; }
    }
}
