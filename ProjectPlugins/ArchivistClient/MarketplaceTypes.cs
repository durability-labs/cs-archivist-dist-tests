using Logging;
using Utils;

namespace ArchivistClient
{
    public static class DefaultStoragePurchase
    {
        public static ByteSize UploadFileSize => 32.MB();
        public static TestToken PricePerBytePerSecond => 1000.TstWei();
        public static TestToken CollateralPerByte => 1.TstWei();
        public static int MinRequiredNumberOfNodes => 4;
        public static int NodeFailureTolerance => 2;
        public static int ProofProbability => 20;
        public static TimeSpan Duration => TimeSpan.FromMinutes(20.0);
        public static TimeSpan Expiry => TimeSpan.FromMinutes(10.0);
    }

    public class StoragePurchaseRequest
    {
        public StoragePurchaseRequest(ContentId cid)
        {
            ContentId = cid;
        }

        public ContentId ContentId { get; }
        public TestToken PricePerBytePerSecond { get; set; } = DefaultStoragePurchase.PricePerBytePerSecond;
        public TestToken CollateralPerByte { get; set; } = DefaultStoragePurchase.CollateralPerByte;
        public int MinRequiredNumberOfNodes { get; set; } = DefaultStoragePurchase.MinRequiredNumberOfNodes;
        public int NodeFailureTolerance { get; set; } = DefaultStoragePurchase.NodeFailureTolerance;
        public int ProofProbability { get; set; } = DefaultStoragePurchase.ProofProbability;
        public TimeSpan Duration { get; set; } = DefaultStoragePurchase.Duration;
        public TimeSpan Expiry { get; set; } = DefaultStoragePurchase.Expiry;

        public void Log(ILog log)
        {
            log.Log($"Requesting storage for: {ContentId.Id}... (" +
                $"pricePerBytePerSecond: {PricePerBytePerSecond}, " +
                $"collateralPerByte: {CollateralPerByte}, " +
                $"minRequiredNumberOfNodes: {MinRequiredNumberOfNodes}, " +
                $"nodeFailureTolerance: {NodeFailureTolerance}, " +
                $"proofProbability: {ProofProbability}, " +
                $"expiry: {Time.FormatDuration(Expiry)}, " +
                $"duration: {Time.FormatDuration(Duration)})");
        }
    }

    public class StoragePurchase
    {
        public StoragePurchaseState State { get; set; } = StoragePurchaseState.Unknown;
        public string Error { get; set; } = string.Empty;
        public StorageRequest Request { get; set; } = null!;

        public bool IsCancelled => State == StoragePurchaseState.Cancelled;
        public bool IsError => State == StoragePurchaseState.Errored;
        public bool IsFinished => State == StoragePurchaseState.Finished;
        public bool IsStarted => State == StoragePurchaseState.Started;
        public bool IsSubmitted => State == StoragePurchaseState.Submitted;
    }

    public enum StoragePurchaseState
    {
        Cancelled = 0,
        Errored = 1,
        Failed = 2,
        Finished = 3,
        Pending = 4,
        Started = 5,
        Submitted = 6,
        Unknown = 7,
    }

    public class StorageRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public StorageAsk Ask { get; set; } = null!;
        public StorageContent Content { get; set; } = null!;
        public long Expiry { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }

    public class StorageAsk
    {
        public long Slots { get; set; }
        public long SlotSize { get; set; }
        public long Duration { get; set; }
        public string ProofProbability { get; set; } = string.Empty;
        public string PricePerBytePerSecond { get; set; } = string.Empty;
        public long MaxSlotLoss { get; set; }
    }

    public class StorageContent
    {
        public string Cid { get; set; } = string.Empty;
        //public ErasureParameters Erasure { get; set; }
        //public PoRParameters Por { get; set; }
    }

    public class CreateStorageAvailability
    {
        public CreateStorageAvailability(TimeSpan maxDuration, DateTime untilUtc, TestToken minPricePerBytePerSecond, TestToken maxCollateralPerByte)
        {
            MaxDuration = maxDuration;
            UntilUtc = untilUtc;
            MinPricePerBytePerSecond = minPricePerBytePerSecond;
            MaxCollateralPerByte = maxCollateralPerByte;
        }

        public TimeSpan MaxDuration { get; }
        public DateTime UntilUtc { get; }
        public TestToken MinPricePerBytePerSecond { get; }
        public TestToken MaxCollateralPerByte { get; }

        public void Log(ILog log)
        {
            log.Log($"Create storage Availability: (" +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " +
                $"untilUtc: {Time.FormatTimestamp(UntilUtc)}, " +
                $"minPricePerBytePerSecond: {MinPricePerBytePerSecond}, " +
                $"maxCollateralPerByte: {MaxCollateralPerByte})");
        }
    }

    public class StorageAvailability
    {
        public StorageAvailability(TimeSpan maxDuration, DateTime untilUtc, TestToken minPricePerBytePerSecond, TestToken maxCollateralPerByte)
        {
            MaxDuration = maxDuration;
            UntilUtc = untilUtc;
            MinPricePerBytePerSecond = minPricePerBytePerSecond;
            MaxCollateralPerByte = maxCollateralPerByte;
        }

        public TimeSpan MaxDuration { get; }
        public DateTime UntilUtc { get; }
        public TestToken MinPricePerBytePerSecond { get; }
        public TestToken MaxCollateralPerByte { get; } 

        public void Log(ILog log)
        {
            log.Log($"Storage Availability: (" +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPricePerBytePerSecond: {MinPricePerBytePerSecond}, " +
                $"maxCollateralPerByte: {MaxCollateralPerByte}, " +
                $"untilUtc: {Time.FormatTimestamp(UntilUtc)})");
        }
    }

    public class StorageSlot
    {
        public StorageSlot(string id, StorageRequest request, long slotIndex)
        {
            Id = id;
            Request = request;
            SlotIndex = slotIndex;
        }
        
        public string Id { get; }
        public StorageRequest Request { get; }
        public long SlotIndex { get; }

        public void Log(ILog log)
        {
            log.Log($"Storage Slot: (" +
                $"id: {Id}, " +
                $"slotIndex: {SlotIndex}, " +
                $"request: {Request.Id})");
        }
    }
}
