using ArchivistClient;
using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class NodeOperator : INodeProcessHandler
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
            OnNodeAction(filePath, a => a.CreateNewPurchase());
        }

        public void ExtendPurchase(string filePath)
        {
            OnNodeAction(filePath, a => a.ExtendPurchase());
        }

        private void OnNodeAction(string filePath, Action<NodeAction> action)
        {
            var localFilename = Path.GetFileName(filePath);
            var entry = folderStatus.GetEntry(localFilename);

            dispatcher.OnNode(node =>
            {
                var nodeAction = new NodeAction(log, filePath, node, entry, appEventHandler);
                action(nodeAction);
            },
            whenDone: folderStatus.SaveChanges);
        }

        public class NodeAction
        {
            private readonly ILog log;
            private readonly string filePath;
            private readonly ArchivistWrapper node;
            private readonly FileStatus entry;
            private readonly IAppEventHandler appEventHandler;

            public NodeAction(ILog log, string filePath, ArchivistWrapper node, FileStatus entry, IAppEventHandler appEventHandler)
            {
                this.log = log;
                this.filePath = filePath;
                this.node = node;
                this.entry = entry;
                this.appEventHandler = appEventHandler;
            }

            public void CreateNewPurchase()
            {
                var cid = UploadFile();
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
                }
                catch (Exception exc)
                {
                    log.Error("Failed to start new purchase: " + exc);
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
                }
                catch (Exception exc)
                {
                    log.Error("Failed to start new purchase: " + exc);
                    throw;
                }
            }

            private string UploadFile()
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

                    WaitForSubmitted(request);
                    WaitForStarted(request);

                    appEventHandler.OnPurchaseSuccess();
                    Log($"Successfully started new purchase: '{request.PurchaseId}'");
                }
                catch
                {
                    entry.EncodedCid = string.Empty;
                    appEventHandler.OnPurchaseFailure();
                    throw;
                }
            }

            private void WaitForSubmittedToStarted(StoragePurchase purchase)
            {
                try
                {
                    if (purchase.IsStarted) return;

                    var expirySeconds = Convert.ToInt64(purchase.Request.Expiry);
                    var expiry = TimeSpan.FromSeconds(expirySeconds);
                    Log($"Request was submitted but not started yet. Waiting {Time.FormatDuration(expiry)} to start or expire...");

                    var limit = DateTime.UtcNow + expiry;
                    while (DateTime.UtcNow < limit)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                        var update = GetPurchase(purchase.Request.Id);
                        if (update != null)
                        {
                            if (update.IsStarted)
                            {
                                Log("Request successfully started.");
                                return;
                            }
                            else if (!update.IsSubmitted)
                            {
                                Log("Request failed to start. State: " + update.State);
                                entry.EncodedCid = string.Empty;
                                entry.PurchaseId = string.Empty;
                                stats.StorageRequestStats.FailedToStart++;
                                saveHandler.SaveChanges();
                                return;
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    resultHandler.OnPurchaseFailure();
                    Log($"Exception in {nameof(WaitForSubmittedToStarted)}: {exc}");
                    throw;
                }
            }

            private void WaitForSubmitted(IStoragePurchaseContract request)
            {
                try
                {
                    request.WaitForStorageContractSubmitted();
                }
                catch
                {
                    stats.StorageRequestStats.FailedToSubmit++;
                    throw;
                }
            }

            private void WaitForStarted(IStoragePurchaseContract request)
            {
                try
                {
                    request.WaitForStorageContractStarted();
                }
                catch
                {
                    stats.StorageRequestStats.FailedToStart++;
                    throw;
                }
            }

            private void Log(string v)
            {
                throw new NotImplementedException();
            }
        }
    }
}
