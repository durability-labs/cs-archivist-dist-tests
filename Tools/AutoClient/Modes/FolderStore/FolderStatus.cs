using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class FolderStatus
    {
        public const string FolderSaverFilename = "foldersaver.json";
        private readonly JsonFile<FolderStatusModel> statusFile;
        private readonly FolderStatusModel status;
        private readonly object statusLock = new object();

        public FolderStatus(App app)
        {
            statusFile = new JsonFile<FolderStatusModel>(app, Path.Combine(app.Config.FolderToStore, FolderSaverFilename));
            status = statusFile.Load();
        }

        public void SaveChanges()
        {
            lock (statusLock)
            {
                statusFile.Save(status);
            }
        }

        public FileStatus GetEntry(string localFilename)
        {
            lock (statusLock)
            {
                return GetEntryInternal(localFilename);
            }
        }

        public FileStatus? Get(Func<FileStatus, bool> selector)
        {
            lock (statusLock)
            {
                var matches = status.Files.Where(selector).ToArray();
                if (matches.Length > 0) return matches.GetOneRandom();
            }

            return null;
        }

        private FileStatus GetEntryInternal(string localFilename)
        {
            var entry = status.Files.SingleOrDefault(f => f.Filename == localFilename);
            if (entry != null) return entry;
            var newEntry = new FileStatus
            {
                Filename = localFilename
            };
            status.Files.Add(newEntry);
            return newEntry;
        }
    }
}
