using ArchivistClient.Hooks;
using FileUtils;
using Logging;
using WebUtils;

namespace ArchivistClient
{
    public class ArchivistNodeFactory
    {
        private readonly ILog log;
        private readonly IFileManager fileManager;
        private readonly ArchivistHooksFactory hooksFactory;
        private readonly IHttpFactory httpFactory;
        private readonly IProcessControlFactory processControlFactory;

        public ArchivistNodeFactory(ILog log, IFileManager fileManager, ArchivistHooksFactory hooksFactory, IHttpFactory httpFactory, IProcessControlFactory processControlFactory)
        {
            this.log = log;
            this.fileManager = fileManager;
            this.hooksFactory = hooksFactory;
            this.httpFactory = httpFactory;
            this.processControlFactory = processControlFactory;
        }

        public ArchivistNodeFactory(ILog log, HttpFactory httpFactory, string dataDir)
            : this(log, new FileManager(log, dataDir), new ArchivistHooksFactory(), httpFactory, new DoNothingProcessControlFactory())
        {
        }

        public ArchivistNodeFactory(ILog log, string dataDir)
            : this(log, new HttpFactory(log), dataDir)
        {
        }

        public IArchivistNode CreateArchivistNode(IArchivistInstance instance)
        {
            var nodeLog = new LogPrefixer(log, $"({instance.Name}) ");
            var processControl = processControlFactory.CreateProcessControl(instance);
            var access = new ArchivistAccess(nodeLog, httpFactory, processControl, instance);
            var hooks = hooksFactory.CreateHooks(access.GetName());
            var marketplaceAccess = CreateMarketplaceAccess(instance, nodeLog, access, hooks);
            var node =  new ArchivistNode(nodeLog, access, fileManager, marketplaceAccess, hooks);
            node.Initialize();
            return node;
        }

        private IMarketplaceAccess CreateMarketplaceAccess(IArchivistInstance instance, ILog nodeLog, ArchivistAccess access, IArchivistNodeHooks hooks)
        {
            if (instance.EthAccount == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(nodeLog, access, hooks);
        }
    }
}
