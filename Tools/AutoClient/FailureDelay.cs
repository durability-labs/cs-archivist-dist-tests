using AutoClient.Modes.FolderStore;
using Logging;
using Utils;

namespace AutoClient
{
    public class FailureDelay : IAppEventHandler
    {
        private readonly TimeSpan max = TimeSpan.FromMinutes(15);
        private readonly TimeSpan min = TimeSpan.FromSeconds(1);
        private readonly ILog log;
        private TimeSpan delay;

        public FailureDelay(ILog log)
        {
            delay = min;
            this.log = log;
        }

        public void ApplyDelay()
        {
            if (delay > TimeSpan.FromSeconds(30)) log.Log($"Delay: {Time.FormatDuration(delay)}");
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
