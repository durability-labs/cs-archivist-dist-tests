using ChainStateAPI.Controllers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChainStateAPI.Database
{
    public class DbContractsContext : BaseContext
    {
        protected override string DatabaseName => "events";

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

        public DateTime CreationUtc { get; set; }
        public DateTime ExpiryUtc { get; set; }
        public DateTime FinishedUtc { get; set; }

        public ICollection<ContractEvent> ContractEvents { get; set; } = new List<ContractEvent>();
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
        public ulong Expiry { get; set; }
        public ContractAsk Ask { get; set; } = new();
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
