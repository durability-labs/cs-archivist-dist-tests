namespace AutoClient.Modes.FolderStore
{
    [Serializable]
    public class FolderStatusModel
    {
        public List<FileStatus> Files { get; set; } = new List<FileStatus>();
        public Stats Stats { get; set; } = new Stats();
        public string Padding { get; set; } = string.Empty;
    }

    [Serializable]
    public class FileStatus
    {
        public string Filename { get; set; } = string.Empty;
        public string EncodedCid { get; set; } = string.Empty;
        public int PurchaseNodes { get; set; }
        public int PurchaseTolerance { get; set; }
        public DateTime PurchaseFinishedUtc { get; set; } = DateTime.MinValue;
    }

    [Serializable]
    public class Stats
    {
        public int SuccessfulUploads { get; set; }
        public int FailedUploads { get; set; }
        public StorageRequestStats StorageRequestStats { get; set; } = new StorageRequestStats();
    }

    [Serializable]
    public class StorageRequestStats
    {
        public int FailedToCreate { get; set; }
        public int FailedToSubmit { get; set; }
        public int FailedToStart { get; set; }
        public int SuccessfullyStarted { get; set; }
    }
}
