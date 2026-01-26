using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class FileProcessor : IFilePathHandler
    {
        private readonly App app;
        private readonly FolderStatus folderStatus;
        private readonly INodeOperations handler;
        private readonly IAppEventHandler appEventHandler;

        public FileProcessor(App app, FolderStatus folderStatus, INodeOperations handler, IAppEventHandler appEventHandler)
        {
            this.app = app;
            this.folderStatus = folderStatus;
            this.handler = handler;
            this.appEventHandler = appEventHandler;
        }

        public void OnFile(string filePath)
        {
            var localFilename = Path.GetFileName(filePath);
            var entry = folderStatus.GetEntry(localFilename);

            var process = new FileProcess(app.Log, filePath, entry, handler);
            appEventHandler.OnFileProcessStarted();
            process.Run();
        }

        public class FileProcess
        {
            private readonly ILog log;
            private readonly string filePath;
            private readonly FileStatus entry;
            private readonly INodeOperations handler;

            public FileProcess(ILog log, string filePath, FileStatus entry, INodeOperations handler)
            {
                this.log = log;
                this.filePath = filePath;
                this.entry = entry;
                this.handler = handler;
            }

            public void Run()
            {
                if (!HasPreviousPurchaseInfo())
                {
                    Log("No previous purchase info found. Creating new purchase...");
                    handler.CreateNewPurchase(filePath);
                    return;
                }

                if (IsPurchaseRunning())
                {
                    Log("Purchase is running. Nothing to do.");
                    return;
                }

                Log("Previous purchase has finished. Creating new purchase...");
                handler.CreateNewPurchase(filePath);
            }

            private bool IsPurchaseRunning()
            {
                return DateTime.UtcNow < EffectivePurchaseFinishedUtc();
            }

            private bool HasPreviousPurchaseInfo()
            {
                if (string.IsNullOrEmpty(entry.EncodedCid)) return false;
                if (entry.PurchaseNodes < 3) return false;
                if (entry.PurchaseTolerance < 1) return false;
                if (entry.PurchaseFinishedUtc == DateTime.MinValue) return false;
                return true;
            }

            private void Log(string v)
            {
                log.Log(v);
            }
            
            private DateTime EffectivePurchaseFinishedUtc()
            {
                // In the last 30 minutes, we consider the purchase already finished,
                // Because there's not enough time to renew.
                return entry.PurchaseFinishedUtc - TimeSpan.FromMinutes(30);
            }
        }
    }
}
