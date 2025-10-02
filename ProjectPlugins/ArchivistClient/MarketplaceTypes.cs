using Logging;
using Utils;

namespace ArchivistClient
{
    public class StoragePurchaseRequest
    {
        public StoragePurchaseRequest(ContentId cid)
        {
            ContentId = cid;
        }

        public ContentId ContentId { get; }
        public TestToken PricePerBytePerSecond { get; set; } = 1000.TstWei();
        public TestToken CollateralPerByte { get; set; } = 1.TstWei();
        public uint MinRequiredNumberOfNodes { get; set; } = 4;
        public uint NodeFailureTolerance { get; set; } = 2;
        public int ProofProbability { get; set; } = 20;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(20.0);
        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(10.0);

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
        public CreateStorageAvailability(ByteSize totalSpace, TimeSpan maxDuration, TestToken minPricePerBytePerSecond, TestToken totalCollateral)
        {
            TotalSpace = totalSpace;
            MaxDuration = maxDuration;
            MinPricePerBytePerSecond = minPricePerBytePerSecond;
            TotalCollateral = totalCollateral;
        }

        public ByteSize TotalSpace { get; }
        public TimeSpan MaxDuration { get; }
        public TestToken MinPricePerBytePerSecond { get; }
        public TestToken TotalCollateral { get; }

        public void Log(ILog log)
        {
            log.Log($"Create storage Availability: (" +
                $"totalSize: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " +
                $"minPricePerBytePerSecond: {MinPricePerBytePerSecond}, " +
                $"totalCollateral: {TotalCollateral})");
        }
    }

    public class StorageAvailability
    {
        public StorageAvailability(string id, ByteSize totalSpace, TimeSpan maxDuration, TestToken minPricePerBytePerSecond, TestToken totalCollateral, ByteSize freeSpace)
        {
            Id = id;
            TotalSpace = totalSpace;
            MaxDuration = maxDuration;
            MinPricePerBytePerSecond = minPricePerBytePerSecond;
            TotalCollateral = totalCollateral;
            FreeSpace = freeSpace;
        }

        public string Id { get; }
        public ByteSize TotalSpace { get; }
        public TimeSpan MaxDuration { get; }
        public TestToken MinPricePerBytePerSecond { get; }
        public TestToken TotalCollateral { get; } 
        public ByteSize FreeSpace { get; }

        public void Log(ILog log)
        {
            log.Log($"Storage Availability: (" +
                $"id: {Id}, " +
                $"totalSize: {TotalSpace}, " +
                $"maxDuration: {Time.FormatDuration(MaxDuration)}, " + 
                $"minPricePerBytePerSecond: {MinPricePerBytePerSecond}, " +
                $"totalCollateral: {TotalCollateral}, " +
                $"freeSpace: {FreeSpace})");
        }
    }

    public class AvailabilityReservation
    {
        public AvailabilityReservation(string id, string availabilityId, long size, string requestId, long slotIndex, int validUntil)
        {
            Id = id;
            AvailabilityId = availabilityId;
            Size = size.Bytes();
            RequestId = requestId;
            SlotIndex = slotIndex;
            ValidUntil = Time.ToUtcDateTime(validUntil);
        }

        public string Id { get; }
        public string AvailabilityId { get; }
        public ByteSize Size { get; }
        public string RequestId { get; }
        public long SlotIndex { get; }
        public DateTime ValidUntil { get; }

        public void Log(ILog log)
        {
            log.Log($"\tStorage Availability Reservation: (" +
                $"id: {Id}, " +
                $"availabilityId: {AvailabilityId}, " +
                $"size: {Size}, " +
                $"requestId: {RequestId}, " +
                $"slotIndex: {SlotIndex}, " +
                $"validUntil: {Time.FormatTimestamp(ValidUntil)})");
        }
    }
}
