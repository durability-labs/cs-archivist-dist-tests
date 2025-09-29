using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;

namespace NethereumWorkflow
{
    public interface IEventsCollector
    {
        string Name { get; }
        EventABI AbiEvent { get; }
        void CollectMyEvents(List<FilterLog> logs);
    }

    public class EventsCollector<TEvent> : IEventsCollector where TEvent : IEventDTO, new()
    {
        public EventsCollector()
        {
            AbiEvent = ABITypedRegistry.GetEvent<TEvent>();
        }

        void IEventsCollector.CollectMyEvents(List<FilterLog> logs)
        {
            foreach (var l in logs)
            {
                if (AbiEvent.IsLogForEvent(l))
                {
                    Events.Add(l.DecodeEvent<TEvent>());
                }
            }
        }

        public string Name => typeof(TEvent).Name;
        public EventABI AbiEvent { get; }
        public List<EventLog<TEvent>> Events { get; } = new();
    }
}
