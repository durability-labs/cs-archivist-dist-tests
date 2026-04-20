using ArchivistClient;
using GethPlugin;

namespace ArchivistContractsPlugin
{
    public interface IArchivistContractsSetup
    {
        IArchivistContractsSetup WithRpcNode(IGethNode node);
        IArchivistContractsSetup WithVersionInfo(DebugInfoVersion info);
        IArchivistContractsSetup WithRequestsCache(IRequestsCache requestsCache);
        IArchivistContractsSetup WithMaxReservationsOverride(int maxReservations);
    }

    public class ArchivistContractsSetupBuilder : IArchivistContractsSetup
    {
        private int? maxReservationsOverride = null;
        private IRequestsCache requestsCache = new NullRequestsCache();
        private IGethNode? rpcNode = null;
        private DebugInfoVersion? infoVersion = null;

        public IArchivistContractsSetup WithMaxReservationsOverride(int maxReservations)
        {
            maxReservationsOverride = maxReservations;
            return this;
        }

        public IArchivistContractsSetup WithRequestsCache(IRequestsCache requestsCache)
        {
            this.requestsCache = requestsCache;
            return this;
        }

        public IArchivistContractsSetup WithRpcNode(IGethNode node)
        {
            rpcNode = node;
            return this;
        }

        public IArchivistContractsSetup WithVersionInfo(DebugInfoVersion info)
        {
            infoVersion = info;
            return this;
        }

        public ArchivistContractsSetup Build()
        {
            if (rpcNode == null) throw new Exception("ArchivistContracts requires RPC node. Use '.WithRpcNode(...)'");
            if (infoVersion == null) throw new Exception("ArchivistContracts requires Archivist version information. Use '.WithVersionInfo(...)'");
            return new ArchivistContractsSetup(rpcNode, infoVersion, requestsCache, maxReservationsOverride);
        }
    }

    public class ArchivistContractsSetup
    {
        public ArchivistContractsSetup(IGethNode rpcNode, DebugInfoVersion infoVersion, IRequestsCache requestsCache, int? maxReservationsOverride)
        {
            RpcNode = rpcNode;
            InfoVersion = infoVersion;
            RequestsCache = requestsCache;
            MaxReservationsOverride = maxReservationsOverride;
        }

        public IGethNode RpcNode { get; }
        public DebugInfoVersion InfoVersion { get; }
        public IRequestsCache RequestsCache { get; }
        public int? MaxReservationsOverride { get; }
    }
}
