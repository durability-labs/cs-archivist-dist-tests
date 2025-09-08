using ArchivistContractsPlugin.Marketplace;

namespace ArchivistContractsPlugin
{
    public class ArchivistContractsDeployment
    {
        public ArchivistContractsDeployment(MarketplaceConfig config, string marketplaceAddress, string abi, string tokenAddress)
        {
            Config = config;
            MarketplaceAddress = marketplaceAddress;
            Abi = abi;
            TokenAddress = tokenAddress;
        }

        public MarketplaceConfig Config { get; }
        public string MarketplaceAddress { get; }
        public string Abi { get; }
        public string TokenAddress { get; }
    }
}
