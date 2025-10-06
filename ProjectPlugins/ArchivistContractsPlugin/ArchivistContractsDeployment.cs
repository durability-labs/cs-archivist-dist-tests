using ArchivistContractsPlugin.Marketplace;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractsDeployment
    {
        public ArchivistContractsDeployment(MarketplaceConfig config, string marketplaceAddress, string abi)
        {
            Config = config;
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
        }

        public MarketplaceConfig Config { get; }
        public string MarketplaceAddress { get; }
        public string Abi { get; }
    }
}
