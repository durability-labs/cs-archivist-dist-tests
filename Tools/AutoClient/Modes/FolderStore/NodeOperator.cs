using ArchivistClient;
using Logging;

namespace AutoClient.Modes.FolderStore
{
    public interface INodeOperations
    {
        void CreateNewPurchase(string filePath);
        void ExtendPurchase(FileStatus fileStatus);
    }

    public class NodeOperator : INodeOperations
    {
        private readonly ILog log;
        private readonly FolderStatus folderStatus;
        private readonly NodeDispatcher dispatcher;
        private readonly IAppEventHandler appEventHandler;

        public NodeOperator(ILog log, FolderStatus folderStatus, NodeDispatcher dispatcher, IAppEventHandler appEventHandler)
        {
            this.log = log;
            this.folderStatus = folderStatus;
            this.dispatcher = dispatcher;
            this.appEventHandler = appEventHandler;
        }

        public void CreateNewPurchase(string filePath)
        {
            OnNodeAction(filePath, a => a.CreateNewPurchase(filePath));
        }

        public void ExtendPurchase(FileStatus fileStatus)
        {
            OnNodeAction(fileStatus, a => a.ExtendPurchase());
        }

        private void OnNodeAction(string filePath, Action<NodeAction> action)
        {
            var localFilename = Path.GetFileName(filePath);
            var entry = folderStatus.GetEntry(localFilename);

            OnNodeAction(entry, action);
        }

        private void OnNodeAction(FileStatus entry, Action<NodeAction> action)
        {
            dispatcher.OnNode(node =>
            {
                var nodeAction = new NodeAction(log, folderStatus, node, entry, appEventHandler);
                action(nodeAction);
            },
            whenDone: folderStatus.SaveChanges);
        }

        public class NodeAction
        {
            private readonly ILog log;
            private readonly FolderStatus folderStatus;
            private readonly ArchivistWrapper node;
            private readonly FileStatus entry;
            private readonly IAppEventHandler appEventHandler;

            public NodeAction(ILog log, FolderStatus folderStatus, ArchivistWrapper node, FileStatus entry, IAppEventHandler appEventHandler)
            {
                this.log = log;
                this.folderStatus = folderStatus;
                this.node = node;
                this.entry = entry;
                this.appEventHandler = appEventHandler;
            }

            public void CreateNewPurchase(string filePath)
            {
                var cid = UploadFile(filePath);
                CreatePurchase(cid);
            }

            public void ExtendPurchase()
            {
                Log("Extending existing purchase...");
                try
                {
                    var request = node.ExtendStorage(
                        new ContentId(entry.EncodedCid),
                        entry.PurchaseNodes,
                        entry.PurchaseTolerance
                    );
                    HandleNewRequest(request);
                    appEventHandler.OnPurchaseExtended();
                }
                catch (Exception exc)
                {
                    log.Error("Failed to extend purchase: " + exc);
                    appEventHandler.OnPurchaseFailure();
                    throw;
                }
            }

            private void CreatePurchase(string cid)
            {
                Log("Creating new purchase...");
                try
                {
                    var request = node.RequestStorage(new ContentId(cid));
                    HandleNewRequest(request);
                    appEventHandler.OnPurchaseSuccess();
                }
                catch (Exception exc)
                {
                    log.Error("Failed to start new purchase: " + exc);
                    appEventHandler.OnPurchaseFailure();
                    throw;
                }
            }

            private string UploadFile(string filePath)
            {
                Log("Uploading file...");
                try
                {
                    var cid = node.UploadFile(filePath).Id;
                    appEventHandler.OnUploadSuccess();
                    Log($"Successfully uploaded. BasicCid: '{cid}'");
                    return cid;
                }
                catch (Exception exc)
                {
                    appEventHandler.OnUploadFailure();
                    log.Error("Failed to upload: " + exc);
                    throw;
                }
            }

            private void HandleNewRequest(IStoragePurchaseContract request)
            {
                try
                {
                    entry.EncodedCid = request.ContentId.Id;
                    entry.PurchaseNodes = request.Purchase.MinRequiredNumberOfNodes;
                    entry.PurchaseTolerance = request.Purchase.NodeFailureTolerance;
                    entry.PurchaseFinishedUtc = DateTime.UtcNow + request.Purchase.Duration;

                    request.WaitForStorageContractSubmitted();
                    request.WaitForStorageContractStarted();

                    folderStatus.SaveChanges();
                    Log($"Successfully started new purchase: '{request.PurchaseId}'");
                }
                catch
                {
                    entry.EncodedCid = string.Empty;
                    entry.PurchaseNodes = 0;
                    entry.PurchaseTolerance = 0;
                    folderStatus.SaveChanges();
                    appEventHandler.OnPurchaseFailure();
                    throw;
                }
            }

            private void Log(string v)
            {
                log.Log(v);
            }
        }
    }
}
