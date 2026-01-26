using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public interface IFilePathHandler
    {
        void OnFile(string filePath);
    }

    public class FolderIterator
    {
        private readonly App app;
        private readonly ILog log;
        private readonly IFilePathHandler handler;
        private readonly List<string> folderFiles = new List<string>();
        private readonly object _lock = new object();

        public FolderIterator(App app, IFilePathHandler handler)
        {
            this.app = app;
            log = new LogPrefixer(app.Log, "(FolderIter)");
            this.handler = handler;
        }

        public void Initialize()
        {
            Log("Starting...");

            if (!Directory.Exists(app.Config.FolderToStore)) throw new Exception("Path does not exist: " + app.Config.FolderToStore);
            var files = Directory.GetFiles(app.Config.FolderToStore);
            if (!files.Any()) throw new Exception("No files found in " + app.Config.FolderToStore);

            lock (_lock)
            {
                folderFiles.AddRange(files);
                Log($"Queued {folderFiles.Count} files");
            }
        }

        public bool IsFinished
        {
            get
            {
                return !folderFiles.Any();
            }
        }

        public void Step()
        {
            var folderFile = string.Empty;
            if (app.Cts.IsCancellationRequested) return;
            lock (_lock)
            {
                if (folderFile.Length == 0) return;
                folderFile = folderFiles[0];
                folderFiles.RemoveAt(0);
            }

            Log($"File: '{folderFile}'");
            ProcessFile(folderFile);
        }

        private void ProcessFile(string folderFile)
        {
            if (folderFile.ToLowerInvariant().EndsWith(FolderStatus.FolderSaverFilename)) return;
            if (!File.Exists(folderFile)) return;
            var fileSize = new FileInfo(folderFile).Length;
            if (fileSize < 1.MB().SizeInBytes) return;
            
            handler.OnFile(folderFile);
        }

        private void Log(string v)
        {
            log.Log(v);
        }
    }
}
