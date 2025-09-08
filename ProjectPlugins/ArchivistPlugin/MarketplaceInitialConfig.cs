using ArchivistContractsPlugin;
using GethPlugin;

namespace ArchivistPlugin
{
    public class MarketplaceInitialConfig
    {
        public MarketplaceInitialConfig(MarketplaceSetup marketplaceSetup, IGethNode gethNode, IArchivistContracts archivistContracts)
        {
            MarketplaceSetup = marketplaceSetup;
            GethNode = gethNode;
            ArchivistContracts = archivistContracts;
        }

        public MarketplaceSetup MarketplaceSetup { get; }
        public IGethNode GethNode { get; }
        public IArchivistContracts ArchivistContracts { get; }
    }
}
