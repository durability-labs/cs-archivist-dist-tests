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
        private readonly IFilePathHandler handler;
        private readonly List<string> folderFiles = new List<string>();
        private readonly object _lock = new object();

        public FolderIterator(App app, IFilePathHandler handler)
        {
            this.app = app;
            this.handler = handler;
        }

        public void Initialize()
        {
            Log("Starting FolderIterator...");

            if (!Directory.Exists(app.Config.FolderToStore)) throw new Exception("Path does not exist: " + app.Config.FolderToStore);
            var files = Directory.GetFiles(app.Config.FolderToStore);
            if (!files.Any()) throw new Exception("No files found in " + app.Config.FolderToStore);

            lock (_lock)
            {
                folderFiles.AddRange(files);
            }
        }

        public bool IsFinished
        {
            get
            {
                lock (_lock) return !folderFiles.Any();
            }
        }

        public void Step()
        {
            var folderFile = string.Empty;
            lock (_lock)
            {

            }


            foreach (var folderFile in folderFiles)
            {
                if (app.Cts.IsCancellationRequested)
                {
                    Log("Iteration cancelled.");
                    return;
                }

                if (!folderFile.ToLowerInvariant().EndsWith(FolderStatus.FolderSaverFilename))
                {
                    var fileSize = (new FileInfo(folderFile)).Length;
                    if (fileSize > 1.MB().SizeInBytes)
                    {
                        handler.OnFile(folderFile);
                    }
                }
            }
            Log("All files processed.");
        }

        private void Log(string v)
        {
            app.Log.Log(v);
        }
    }
}
