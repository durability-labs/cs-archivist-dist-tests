namespace AutoClient.Modes.FolderStore
{
    public class FolderSaveResultHandler : IFileSaverResultHandler
    {
        private readonly App app;
        private readonly FileStatus entry;

        public FolderSaveResultHandler(App app, FileStatus entry)
        {
            this.app = app;
            this.entry = entry;
        }

        public void OnProcessStart()
        {
        }

        public void OnPurchaseFailure()
        {
            app.Log.Error($"Failed to store {FolderSaver.FolderSaverFilename} :|");
        }

        public void OnPurchaseSuccess()
        {
            if (!string.IsNullOrEmpty(entry.EncodedCid))
            {
                var cidsFile = Path.Combine(app.Config.DataPath, "cids.log");
                File.AppendAllLines(cidsFile, [entry.EncodedCid]);
                app.Log.Log($"!!! {FolderSaver.FolderSaverFilename} saved to CID '{entry.EncodedCid}' !!!");
            }
            else
            {

                app.Log.Error($"Foldersaver entry didn't have encoded CID somehow :|");
            }
        }

        public void OnUploadFailure()
        {
        }

        public void OnUploadSuccess()
        {
        }
    }
}
