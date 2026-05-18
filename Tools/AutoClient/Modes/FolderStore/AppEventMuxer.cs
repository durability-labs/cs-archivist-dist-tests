namespace AutoClient.Modes.FolderStore
{
    public interface IAppEventHandler
    {
        void OnFileProcessStarted();
        void OnUploadSuccess();
        void OnUploadFailure();
        void OnPurchaseNewSuccess();
        void OnPurchaseExtendSuccess();
        void OnPurchaseNewFailure();
        void OnPurchaseExtendFailure();
    }

    public class AppEventMuxer : IAppEventHandler
    {
        private readonly IAppEventHandler[] handlers;

        public AppEventMuxer(params IAppEventHandler[] handlers)
        {
            this.handlers = handlers;
        }

        public void OnFileProcessStarted()
        {
            foreach (var h in handlers) h.OnFileProcessStarted();
        }

        public void OnPurchaseExtendFailure()
        {
            foreach (var h in handlers) h.OnPurchaseExtendFailure();
        }

        public void OnPurchaseExtendSuccess()
        {
            foreach (var h in handlers) h.OnPurchaseExtendSuccess();
        }

        public void OnPurchaseNewFailure()
        {
            foreach (var h in handlers) h.OnPurchaseNewFailure();
        }

        public void OnPurchaseNewSuccess()
        {
            foreach (var h in handlers) h.OnPurchaseNewSuccess();
        }

        public void OnUploadFailure()
        {
            foreach (var h in handlers) h.OnUploadFailure();
        }

        public void OnUploadSuccess()
        {
            foreach (var h in handlers) h.OnUploadSuccess();
        }
    }
}
