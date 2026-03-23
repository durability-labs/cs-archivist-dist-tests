using ChainStateAPI.Controllers;

namespace ChainStateAPI.Services
{
    public interface IEventTypeService
    {
        EventType[] GetEventTypes();
    }

    public class EventTypeService : IEventTypeService
    {
        private readonly EventType[] eventTypes;

        public EventTypeService()
        {
            eventTypes = [
                CreateEventType<StorageRequested>(101),
                CreateEventType<ContractStarted>(102),
                CreateEventType<ContractFailed>(103),
                CreateEventType<SlotFilled>(104),
                CreateEventType<SlotFreed>(105),
                CreateEventType<SlotReservationsFull>(106),
                //CreateEventType<ProofSubmitted>(107),
                CreateEventType<ContractFinished>(108),
            ];
        }

        public EventType[] GetEventTypes()
        {
            return eventTypes;
        }

        private static EventType CreateEventType<T>(int id)
        {
            return new EventType
            {
                Id = id,
                Name = typeof(T).Name,
            };
        }
    }
}
