using Microsoft.AspNetCore.Mvc;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TimespanRequestsController : ControllerBase
    {
        [HttpGet]
        public EventTypesResponse GetEventTypes()
        {
            return new EventTypesResponse();
        }

        [HttpPost]
        public EventsResponse GetEvents([FromBody] EventsRequest request)
        {
            return new EventsResponse();
        }
    }

    public class EventTypesResponse
    {
        public EventType[] Types { get; set; } = Array.Empty<EventType>();
    }

    public class EventType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TimespanRequest
    {
        public DateTime InclFromUtc { get; set; }
        public DateTime ExclToUtc { get; set; }
    }

    public class EventsRequest : TimespanRequest
    {
        public int[] EventTypeIds { get; set; } = Array.Empty<int>();
    }

    public class EventsResponse
    {
        public EventsMoment[] Moments { get; set; } = Array.Empty<EventsMoment>();
    }

    public class EventsMoment
    {
        public DateTime Utc { get; set; }
        public StorageRequested[] StorageRequested { get; set; } = Array.Empty<StorageRequested>();
        public ContractStarted[] ContractStarted { get; set; } = Array.Empty<ContractStarted>();
        public ContractFailed[] ContractFailed { get; set; } = Array.Empty<ContractFailed>();
        public SlotFilled[] SlotFilled { get; set; } = Array.Empty<SlotFilled>();
        public SlotFreed[] SlotFreed { get; set; } = Array.Empty<SlotFreed>();
        public SlotReservationsFull[] SlotReservationsFull { get; set; } = Array.Empty<SlotReservationsFull>();
        //public ProofSubmitted[] ProofSubmitted { get; set; } = Array.Empty<ProofSubmitted>();
        public ContractCancelled[] ContractCancelled { get; set; } = Array.Empty<ContractCancelled>();
        public ContractFinished[] ContractFinished { get; set; } = Array.Empty<ContractFinished>();
    }

    public abstract class ContractEvent
    {
        public string RequestId { get; set; } = string.Empty;
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
