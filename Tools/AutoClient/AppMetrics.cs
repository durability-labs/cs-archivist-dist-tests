using AutoClient.Modes.FolderStore;
using Logging;
using MetricsServer;

namespace AutoClient
{
    public class AppMetrics : IFileSaverResultHandler
    {
        private readonly MetricsServer.MetricsServer server;
        private readonly MetricsEvent processStart;
        private readonly MetricsEvent purchaseSuccess;
        private readonly MetricsEvent purchaseFailed;
        private readonly MetricsEvent uploadSuccess;
        private readonly MetricsEvent uploadFailed;

        public AppMetrics(ILog log, Configuration config)
        {
            server = new MetricsServer.MetricsServer(log, config.MetricsPort, "autoclient");
            server.Start();

            processStart = server.CreateEvent("start", "start processing a file");
            purchaseSuccess = server.CreateEvent("purchase_success", "successfully created and started a new purchase");
            purchaseFailed = server.CreateEvent("purchase_failed", "failed to create and/or start a new purchase");
            uploadSuccess = server.CreateEvent("upload_success", "successfully uploaded a file");
            uploadFailed = server.CreateEvent("upload_failed", "failed to upload a file");
        }

        public void OnProcessStart()
        {
            processStart.Now();
        }

        public void OnPurchaseFailure()
        {
            purchaseFailed.Now();
        }

        public void OnPurchaseSuccess()
        {
            purchaseSuccess.Now();
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
