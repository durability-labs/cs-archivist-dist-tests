using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class PurchaseRenewer
    {
        private readonly App app;
        private readonly FolderStatus folderStatus;
        private readonly INodeOperations handler;
        private DateTime timeoutUntil;

        public PurchaseRenewer(App app, FolderStatus folderStatus, INodeOperations handler)
        {
            this.app = app;
            this.folderStatus = folderStatus;
            this.handler = handler;

            timeoutUntil = DateTime.UtcNow;
        }

        public void Step()
        {
            if (timeoutUntil > DateTime.UtcNow) return;

            var fileStatus = folderStatus.Get(CanExtendPurchase);
            if (fileStatus == null)
            {
                timeoutUntil = DateTime.UtcNow + TimeSpan.FromMinutes(30);
                Log("Found no purchases to renew.");
                return;
            }

            handler.ExtendPurchase(fileStatus);
        }

        private void Log(string v)
        {
            app.Log.Log(v);
        }

        private bool CanExtendPurchase(FileStatus entry)
        {
            if (!HasPreviousPurchaseInfo(entry)) return false;
            if (!IsPurchaseRunning(entry)) return false;

            var extendInterval = new TimeRange(
                from: DateTime.UtcNow,
                to: DateTime.UtcNow + TimeSpan.FromHours(8)
            );

            var finishUtc = EffectivePurchaseFinishedUtc(entry);

            return extendInterval.Includes(finishUtc);
        }

        private bool HasPreviousPurchaseInfo(FileStatus entry)
        {
            if (string.IsNullOrEmpty(entry.EncodedCid)) return false;
            if (entry.PurchaseNodes < 3) return false;
            if (entry.PurchaseTolerance < 1) return false;
            if (entry.PurchaseFinishedUtc == DateTime.MinValue) return false;
            return true;
        }

        private bool IsPurchaseRunning(FileStatus entry)
        {
            return DateTime.UtcNow < EffectivePurchaseFinishedUtc(entry);
        }

        private DateTime EffectivePurchaseFinishedUtc(FileStatus entry)
        {
            // In the last 30 minutes, we consider the purchase already finished,
            // Because there's not enough time to renew.
            return entry.PurchaseFinishedUtc - TimeSpan.FromMinutes(30);
        }
    }
}
