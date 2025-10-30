namespace AutoClient.Modes.FolderStore
{
    public class MuxingFileSaverResultHandler : IFileSaverResultHandler
    {
        private readonly IFileSaverResultHandler[] handlers;

        public MuxingFileSaverResultHandler(params IFileSaverResultHandler[] handlers)
        {
            this.handlers = handlers;
        }

        public void OnProcessStart()
        {
            foreach (var h in handlers) h.OnProcessStart();
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
    }
}
