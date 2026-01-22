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

        public FolderIterator(App app, IFilePathHandler handler)
        {
            this.app = app;
            this.handler = handler;
        }

        public void Run()
        {
            Log("Running FolderIterator...");

            if (!Directory.Exists(app.Config.FolderToStore)) throw new Exception("Path does not exist: " + app.Config.FolderToStore);
            var folderFiles = Directory.GetFiles(app.Config.FolderToStore);
            if (!folderFiles.Any()) throw new Exception("No files found in " + app.Config.FolderToStore);

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
