using ArchivistClient;
using ArchivistClient.Hooks;
using Utils;

namespace ArchivistTests
{
    public class ArchivistLogTrackerProvider  : IArchivistHooksProvider
    {
        private readonly Action<IArchivistNode> addNode;

        public ArchivistLogTrackerProvider(Action<IArchivistNode> addNode)
        {
            this.addNode = addNode;
        }

        // See TestLifecycle.cs DownloadAllLogs()
        public IArchivistNodeHooks CreateHooks(string nodeName)
        {
            return new ArchivistLogTracker(addNode);
        }

        public class ArchivistLogTracker : IArchivistNodeHooks
        {
            private readonly Action<IArchivistNode> addNode;

            public ArchivistLogTracker(Action<IArchivistNode> addNode)
            {
                this.addNode = addNode;
            }

            public void OnFileDownloaded(ByteSize size, ContentId cid)
            {
            }

            public void OnFileDownloading(ContentId cid)
            {
            }

            public void OnFileUploaded(string uid, ByteSize size, ContentId cid)
            {
            }

            public void OnFileUploading(string uid, ByteSize size)
            {
            }

            public void OnNodeStarted(IArchivistNode node, string peerId, string nodeId)
            {
                addNode(node);
            }

            public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
            {
            }

            public void OnNodeStopping()
            {
            }

            public void OnStorageAvailabilityCreated(StorageAvailability response)
            {
            }

            public void OnStorageContractSubmitted(StoragePurchaseContract storagePurchaseContract)
            {
            }

            public void OnStorageContractUpdated(StoragePurchase purchaseStatus)
            {
            }
        }
    }
}
