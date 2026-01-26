namespace AutoClient.Modes.FolderStore
{
    public interface IAppEventHandler
    {
        void OnFileProcessStarted();
        void OnUploadSuccess();
        void OnUploadFailure();
        void OnPurchaseSuccess();
        void OnPurchaseFailure();
        void OnPurchaseExtended();
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

        public void OnPurchaseFailure()
        {
            foreach (var h in handlers) h.OnPurchaseFailure();
        }

        public void OnPurchaseSuccess()
        {
            foreach (var h in handlers) h.OnPurchaseSuccess();
        }

        public void OnUploadFailure()
        {
            foreach (var h in handlers) h.OnUploadFailure();
        }

        public void OnUploadSuccess()
        {
            foreach (var h in handlers) h.OnUploadSuccess();
        }

        public void OnPurchaseExtended()
        {
            foreach (var h in handlers) h.OnPurchaseExtended();
        }
    }
}
