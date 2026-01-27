using Utils;

namespace ArchivistClient.Hooks
{
    public interface IArchivistHooksProvider
    {
        IArchivistNodeHooks CreateHooks(string nodeName);
    }

    public class ArchivistHooksFactory
    {
        public List<IArchivistHooksProvider> Providers { get; } = new List<IArchivistHooksProvider>();

        public IArchivistNodeHooks CreateHooks(string nodeName)
        {
            if (Providers.Count == 0) return new DoNothingArchivistHooks();

            var hooks = Providers.Select(p => p.CreateHooks(nodeName)).ToArray();
            return new MuxingArchivistNodeHooks(hooks);
        }
    }

    public class DoNothingHooksProvider : IArchivistHooksProvider
    {
        public IArchivistNodeHooks CreateHooks(string nodeName)
        {
            return new DoNothingArchivistHooks();
        }
    }

    public class DoNothingArchivistHooks : IArchivistNodeHooks
    {
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
        }

        public void OnNodeStarting(DateTime startUtc, string image, EthAccount? ethAccount)
        {
        }

        public void OnNodeRestarting()
        {
        }

        public void OnNodeRestarted()
        {
        }

        public void OnNodeStopping()
        {
        }

        public void OnStorageAvailabilityCreated()
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
