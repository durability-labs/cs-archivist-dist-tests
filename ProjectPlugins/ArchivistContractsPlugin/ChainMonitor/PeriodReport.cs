using ArchivistContractsPlugin.Marketplace;
using Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Utils;

namespace ArchivistContractsPlugin.ChainMonitor
{
    public class PeriodMonitorResult
    {
        public PeriodMonitorResult(PeriodReport[] reports)
        {
            Reports = reports;
        }

        public PeriodReport[] Reports { get; }
    }

    public class PeriodReport
    {
        public PeriodReport(ProofPeriod period, PeriodRequestReport[] requests, FunctionCallReport[] functionCalls)
        {
            Period = period;
            Requests = requests;
            FunctionCalls = functionCalls;
        }

        public ProofPeriod Period { get; }
        public PeriodRequestReport[] Requests { get; }
        public FunctionCallReport[] FunctionCalls { get; }

        public void Log(ILog log)
        {
            log.Log($"Period report: {Period}");
            log.Log($" - Requests: {Requests.Length}");
            foreach (var r in Requests)
            {
                r.Log(log);
            }
            log.Log($" - Calls: {FunctionCalls.Length}");
            foreach (var f in FunctionCalls)
            {
                log.Log($"   - {f}");
            }
        }

        public int GetNumberOfProofsMissed()
        {
            return Requests.Sum(r => r.GetNumberOfProofsMissed());
        }
    }

    public class PeriodRequestReport
    {
        public static PeriodRequestReport CreatePeriodRequestReport(IArchivistContracts contracts, ulong periodNumber, CurrentRequest currentRequest, bool hasEnded, MarkProofAsMissingFunction[] markCalls)
        {
            // This request existed at the start of the period.
            // Map the information of the slots at the start of the period
            // to the report type, fetching what's missing/might be outdated.
            var slots = currentRequest.Slots.Select(s =>
            {
                var newHost = contracts.GetSlotHost(currentRequest.Request.RequestId, s.Index);
                var canMark = contracts.CanMarkProofAsMissing(s.SlotId, periodNumber);
                var marked = IsProofMarkedAsMissing(markCalls, s.SlotId, periodNumber);
                return new PeriodRequestSlotReport(s, newHost, canMark, marked);
            }).ToArray();

            return new PeriodRequestReport(
                isNew: false,
                hasEnded: hasEnded,
                request: currentRequest.Request,
                slots: slots);
        }

        public static PeriodRequestReport CreatePeriodRequestReport(IArchivistContracts contracts, IChainStateRequest r)
        {
            // This request is newly created during this period.
            // There's no info from the period start to consider.
            // So just fetch the current state and report it.
            var slots = new List<PeriodRequestSlotReport>();
            for (ulong slotIndex = 0; slotIndex < r.Request.Ask.Slots; slotIndex++)
            {
                var idx = Convert.ToInt32(slotIndex);
                var host = r.Hosts.GetHost(idx);
                var slotId = contracts.GetSlotId(r.RequestId, slotIndex);
                slots.Add(new PeriodRequestSlotReport(idx, slotId, host));
            }

            return new PeriodRequestReport(
                isNew: true,
                hasEnded: false,
                request: r,
                slots: slots.ToArray());
        }

        private PeriodRequestReport(bool isNew, bool hasEnded, IChainStateRequest request, PeriodRequestSlotReport[] slots)
        {
            IsNew = isNew;
            HasEnded = hasEnded;
            Request = request;
            Slots = slots;
        }

        public bool IsNew { get; }
        public bool HasEnded { get; }
        public IChainStateRequest Request { get; }
        public PeriodRequestSlotReport[] Slots { get; }

        public int GetNumberOfProofsMissed()
        {
            return Slots.Count(s => s.GetIsProofMissed());
        }

        public void Log(ILog log)
        {
            log.Log($"   - {Request.RequestId.ToHex()} isNew:{IsNew} hasEnded:{HasEnded}");
            foreach (var s in Slots)
            {
                s.Log(log);
            }
        }

        private static bool IsProofMarkedAsMissing(MarkProofAsMissingFunction[] markCalls, byte[] slotId, ulong periodNumber)
        {
            return markCalls.Any(c => c.SlotId.ToHex() == slotId.ToHex() && c.Period == periodNumber);
        }
    }

    public class PeriodRequestSlotReport
    {
        public PeriodRequestSlotReport(CurrentRequestSlot slot, EthAddress? newHost, bool canMarkAsMissing, bool markedAsMissing)
        {
            // This slot existed at the start of the period.
            // We keep the is/will-be information and add the canMark.
            Index = slot.Index;
            SlotId = slot.SlotId;
            Host = newHost; // May have changed!
            IsProofRequired = slot.IsProofRequired;
            WillProofBeRequired = slot.WillProofBeRequired;
            CanMarkAsMissing = canMarkAsMissing;
            MarkedAsMissing = markedAsMissing;
        }

        public PeriodRequestSlotReport(int index, byte[] slotId, EthAddress? host)
        {
            // This slot is newly created during this period.
            // Is/will-be are always false -> if a host fills the slot, proof is already provided.
            Index = index;
            SlotId = slotId;
            Host = host;
            IsProofRequired = false;
            WillProofBeRequired = false;
            CanMarkAsMissing = false;
            MarkedAsMissing = false;
        }

        public int Index { get; }
        public byte[] SlotId { get; }
        public EthAddress? Host { get; }
        public bool IsProofRequired { get; }
        public bool WillProofBeRequired { get; }
        public bool CanMarkAsMissing { get; }
        public bool MarkedAsMissing { get; }

        public void Log(ILog log)
        {
            log.Log($"      - index:{Index} slotId:{SlotId.ToHex()} host:{Host.AsStr()} isProofRequired:{IsProofRequired} willProofBeRequired:{WillProofBeRequired} canMarkAsMissing:{CanMarkAsMissing} markedAsMissing:{MarkedAsMissing}");
        }

        public bool GetIsProofMissed()
        {
            return CanMarkAsMissing || MarkedAsMissing;
        }
    }

    public class FunctionCallReport
    {
        public FunctionCallReport(DateTime utc, ulong blockNumber, string name, string payload)
        {
            Utc = utc;
            BlockNumber = blockNumber;
            Name = name;
            Payload = payload;
        }

        public DateTime Utc { get; }
        public ulong BlockNumber { get; }
        public string Name { get; }
        public string Payload { get; }

        public override string ToString()
        {
            return $"[{Time.FormatTimestamp(Utc)}][{BlockNumber}] {Name} = \"{Payload}\"";
        }
    }
}
