using AutoClient.Modes.FolderStore;
using Logging;
using MetricsServer;

namespace AutoClient
{
    public class AppMetrics : IAppEventHandler
    {
        private readonly MetricsServer.MetricsServer server;
        private readonly MetricsEvent processStart;
        private readonly MetricsEvent purchaseNewSuccess;
        private readonly MetricsEvent purchaseExtendedSuccess;
        private readonly MetricsEvent purchaseNewFailed;
        private readonly MetricsEvent purchaseExtendFailed;
        private readonly MetricsEvent uploadSuccess;
        private readonly MetricsEvent uploadFailed;

        public AppMetrics(ILog log, Configuration config)
        {
            server = new MetricsServer.MetricsServer(log, config.MetricsPort, "autoclient");
            server.Start();

            processStart = server.CreateEvent("start", "start processing a file");
            purchaseNewSuccess = server.CreateEvent("purchase_new_success", "successfully created and started a new purchase");
            purchaseExtendedSuccess = server.CreateEvent("purchase_extended_success", "successfully renewed an existing purchase");
            purchaseNewFailed = server.CreateEvent("purchase_new_failed", "failed to create and/or start a new purchase");
            purchaseExtendFailed = server.CreateEvent("purchase_extend_failed", "failed to create and/or start a extend purchase");
            uploadSuccess = server.CreateEvent("upload_success", "successfully uploaded a file");
            uploadFailed = server.CreateEvent("upload_failed", "failed to upload a file");
        }

        public void OnFileProcessStarted()
        {
            processStart.Now();
        }

        public void OnPurchaseExtendFailure()
        {
            purchaseExtendFailed.Now();
        }

        public void OnPurchaseExtendSuccess()
        {
            purchaseExtendedSuccess.Now();
        }

        public void OnPurchaseNewFailure()
        {
            purchaseNewFailed.Now();
        }

        public void OnPurchaseNewSuccess()
        {
            purchaseNewSuccess.Now();
        }

        public void OnUploadFailure()
        {
            uploadFailed.Now();
        }

        public void OnUploadSuccess()
        {
            uploadSuccess.Now();
        }
    }
}
