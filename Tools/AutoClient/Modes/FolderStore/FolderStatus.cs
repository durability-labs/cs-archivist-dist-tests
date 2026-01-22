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

        //    private void SaveFolderSaverJsonFile()
        //    {
        //        app.Log.Log($"Saving {FolderSaverFilename}...");
        //        var entry = new FileStatus
        //        {
        //            Filename = FolderSaverFilename
        //        };
        //        var folderFile = Path.Combine(app.Config.FolderToStore, FolderSaverFilename);
        //        ApplyPadding(folderFile);
        //        var fileSaver = CreateFileSaver(folderFile, entry, new FolderSaveResultHandler(app, entry));
        //        fileSaver.Process();
        //    }

        //    private const int MinArchivistStorageFilesize = 262144;
        //    private readonly string paddingMessage = $"Archivist currently requires a minimum filesize of {MinArchivistStorageFilesize} bytes for datasets used in storage contracts. " +
        //        $"Anything smaller, and the erasure-coding algorithms used for data durability won't function. Therefore, we apply this padding field to make sure this " +
        //        $"file is larger than the minimal size. The following is pseudo-random: ";

        //    private void ApplyPadding(string folderFile)
        //    {
        //        var info = new FileInfo(folderFile);
        //        var min = MinArchivistStorageFilesize * 2;
        //        if (info.Length < min)
        //        {
        //            var required = Math.Max(1024, min - info.Length);
        //            lock (statusLock)
        //            {
        //                status.Padding = paddingMessage + RandomUtils.GenerateRandomString(required);
        //                statusFile.Save(status);
        //            }
        //        }
        //    }

        //    private FileSaver CreateFileSaver(string folderFile, FileStatus entry)
        //    {
        //        return CreateFileSaver(folderFile, entry, slowModeHandler);
        //    }

        //    private FileSaver CreateFileSaver(string folderFile, FileStatus entry, IFileSaverResultHandler resultHandler)
        //    {
        //        var fixedLength = entry.Filename.PadRight(35);
        //        var prefix = $"[{fixedLength}] ";
        //        var handleMux = new MuxingFileSaverResultHandler(
        //            app.Metrics,
        //            resultHandler
        //        );
        //        return new FileSaver(new LogPrefixer(app.Log, prefix), loadBalancer, status.Stats, folderFile, entry, this, handleMux);
        //    }
    }
}
