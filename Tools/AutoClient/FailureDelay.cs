using AutoClient.Modes.FolderStore;

namespace AutoClient
{
    public class FailureDelay : IAppEventHandler
    {
        private readonly TimeSpan max = TimeSpan.FromMinutes(15);
        private readonly TimeSpan min = TimeSpan.FromSeconds(10);
        private TimeSpan delay;

        public FailureDelay()
        {
            delay = min;
        }

        public void ApplyDelay()
        {
            Thread.Sleep(delay);
        }

        public void OnFileProcessStarted()
        {
        }

        public void OnPurchaseExtended()
        {
            ReduceDelay();
        }

        public void OnPurchaseFailure()
        {
            IncreaseDelay();
        }

        public void OnPurchaseSuccess()
        {
            ReduceDelay();
        }

        public void OnUploadFailure()
        {
        }

        public void OnUploadSuccess()
        {
        }

        private void IncreaseDelay()
        {
            delay = delay * 2.0;
            if (delay > max) delay = max;
        }

        private void ReduceDelay()
        {
            delay = delay * 0.5;
            if (delay < min) delay = min;
        }
    }
}
