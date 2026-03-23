using ChainStateAPI.Database;
using ChainStateAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChainStateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TimespanRequestsController : ControllerBase
    {
        private readonly IEventTypeService eventTypeService;

        public TimespanRequestsController(IEventTypeService eventTypeService)
        {
            this.eventTypeService = eventTypeService;
        }

        [HttpPost]
        public EventsResponse GetEvents([FromBody] EventsRequest request)
        {
            return new EventsResponse();
        }
    }

    public class EventsRequest
    {
        public DateTime InclFromUtc { get; set; }
        public DateTime ExclToUtc { get; set; }
        public string[] RequestIds { get; set; } = Array.Empty<string>();

        public bool StorageRequested { get; set; }
        public bool ContractStarted { get; set; }
        public bool ContractFailed { get; set; }
        public bool SlotFilled { get; set; }
        public bool SlotFreed { get; set; }
        public bool SlotReservationsFull { get; set; }
        //public bool ProofSubmitted { get; set; }
        public bool ContractCancelled { get; set; }
        public bool ContractFinished { get; set; }
    }

    public class EventsResponse
    {
        public EventsMoment[] Moments { get; set; } = Array.Empty<EventsMoment>();
    }

    public class EventsMoment
    {
        public DateTime Utc { get; set; }
        // should these be the db objects? yes compiletime checked, no separation
        // how is the request identified? extra layer? yes clear, no more looping
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
}
