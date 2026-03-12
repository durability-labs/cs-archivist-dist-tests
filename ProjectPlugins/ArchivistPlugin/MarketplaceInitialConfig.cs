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

            // Currently (12-03-2026) devnet and testnet are running on an HTTPS connection to
            // an Arbitrum node which (I think) doesn't support websockets.
            // Should you live in a future where we at Archivist want to support websocket RPC
            // connections: This option is for you.
            UseWebsocketRPC = false;
        }

        public MarketplaceSetup MarketplaceSetup { get; }
        public IGethNode GethNode { get; }
        public IArchivistContracts ArchivistContracts { get; }
        public bool UseWebsocketRPC { get; }
    }
}
