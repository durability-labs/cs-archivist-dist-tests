using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace ChainStateAPI.Database
{
    public class DbContractsContext : BaseContext
    {
        protected override string DatabaseName => "chainstatedb";

        public DbSet<StorageContract> StorageContracts { get; set; }

        public DbSet<StorageRequested> StorageRequestedEvents { get; set; }
        public DbSet<ContractStarted> ContractStartedEvents { get; set; }
        public DbSet<ContractFailed> ContractFailedEvents { get; set; }
        public DbSet<SlotFilled> SlotFilledEvents { get; set; }
        public DbSet<SlotFreed> SlotFreedEvents { get; set; }
        public DbSet<SlotReservationsFull> SlotReservationsFullEvents { get; set; }
        public DbSet<ContractCancelled> ContractCancelledEvents { get; set; }
        public DbSet<ContractFinished> ContractFinished { get; set; }
    }

    [Index(nameof(RequestId), IsUnique = true)]
    public class StorageContract
    {
        [Key]
        public string RequestId { get; set; } = string.Empty;

        public string ClientAddress { get; set; } = string.Empty;

        public int AskId { get; set; }
        public StorageContractAsk Ask { get; set; } = new();
        public byte[] Cid { get; set; } = Array.Empty<byte>();
        public string CidStr { get; set; } = string.Empty;
        public byte[] MerkleRoot { get; set; } = Array.Empty<byte>();

        public DateTime CreationUtc { get; set; }
        public DateTime ExpiryUtc { get; set; }
        public DateTime FinishedUtc { get; set; }

        public ICollection<StorageRequested> StorageRequestedEvents { get; set; } = new List<StorageRequested>();
        public ICollection<ContractStarted> ContractStartedEvents { get; set; } = new List<ContractStarted>();
        public ICollection<ContractFailed> ContractFailedEvents { get; set; } = new List<ContractFailed>();
        public ICollection<SlotFilled> SlotFilledEvents { get; set; } = new List<SlotFilled>();
        public ICollection<SlotFreed> SlotFreedEvents { get; set; } = new List<SlotFreed>();
        public ICollection<SlotReservationsFull> SlotReservationsFullEvents { get; set; } = new List<SlotReservationsFull>();
        public ICollection<ContractCancelled> ContractCancelledEvents { get; set; } = new List<ContractCancelled>();
        public ICollection<ContractFinished> ContractFinishedEvents { get; set; } = new List<ContractFinished>();
    }

    [Index(nameof(AskId), IsUnique = true)]
    public class StorageContractAsk
    {
        [Key]
        public int AskId { get; set; }

        public ulong Expiry { get; set; }
        public BigInteger ProofProbability { get; set; }
        public BigInteger PricePerBytePerSecond { get; set; }
        public BigInteger CollateralPerByte { get; set; }
        public ulong Slots { get; set; }
        public ulong SlotSize { get; set; }
        public ulong Duration { get; set; }
        public ulong MaxSlotLoss { get; set; }
    }

    [Index(nameof(EventId), IsUnique = true)]
    public abstract class ContractEvent
    {
        [Key]
        public int EventId { get; set; }

        public DateTime Utc { get; set; }
        public ulong BlockNumber { get; set; }

        [ForeignKey(nameof(StorageContract))]
        public string RequestId { get; set; } = string.Empty;
        public StorageContract StorageContract { get; set; } = null!;
    }

    public class StorageRequested : ContractEvent
    {
        public int AskId { get; set; }
        public StorageContractAsk Ask { get; set; } = new();
    }

    public class ContractStarted : ContractEvent
    {
    }

    public class ContractFailed : ContractEvent
    {
    }

    public abstract class SlotEvent : ContractEvent
    {
        public ulong SlotIndex { get; set; }
    }

    public class SlotFilled : SlotEvent
    {
        public string HostAddress { get; set; } = string.Empty;
    }

    public class SlotFreed : SlotEvent
    {
    }

    public class SlotReservationsFull : SlotEvent
    {
    }

    //public class ProofSubmitted
    //{
    //    public byte[] Id { get; set; } = Array.Empty<byte>();
    //}

    public class ContractCancelled : ContractEvent
    {
    }

    public class ContractFinished : ContractEvent
    {
    }
}
