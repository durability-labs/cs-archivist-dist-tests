using Logging;

namespace AutoClient
{
    public class Performance
    {
        private readonly ILog log;

        public Performance(ILog log)
        {
            this.log = log;
        }

        public void DownloadFailed(Exception ex)
        {
            Log($"Download failed: {ex}");
        }

        public void DownloadSuccessful(long? size, TimeSpan time)
        {
            if (!size.HasValue) return;

            long milliseconds = Convert.ToInt64(time.TotalMilliseconds);
            if (milliseconds < 1) milliseconds = 1;
            long bytesPerSecond = 1000 * (size.Value / milliseconds);
            Log($"Download successful: {bytesPerSecond} bytes per second");
        }

        public void StorageContractCancelled()
        {
            Log("Contract cancelled");
        }

        public void StorageContractErrored(string error)
        {
            Log($"Contract errored: {error}");
        }

        public void StorageContractFinished()
        {
            Log("Contract finished");
        }

        public void StorageContractStarted()
        {
            Log("Contract started");
        }

        public void UploadFailed(Exception ex)
        {
            Log($"Upload failed: {ex}");
        }

        public void UploadSuccessful(long size, TimeSpan time)
        {
            long milliseconds = Convert.ToInt64(time.TotalMilliseconds);
            if (milliseconds < 1) milliseconds = 1;
            long bytesPerSecond = 1000 * (size / milliseconds);
            Log($"Upload successful: {bytesPerSecond} bytes per second");
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
