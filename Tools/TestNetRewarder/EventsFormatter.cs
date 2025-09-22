using BlockchainUtils;
using ArchivistContractsPlugin.ChainMonitor;
using ArchivistContractsPlugin.Marketplace;
using DiscordRewards;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Globalization;
using System.Numerics;
using Utils;

namespace TestNetRewarder
{
    public class EventsFormatter : IChainStateChangeHandler
    {
        private static readonly string nl = Environment.NewLine;
        private readonly List<ChainEventMessage> events = new List<ChainEventMessage>();
        private readonly List<string> errors = new List<string>();
        private readonly EmojiMaps emojiMaps = new EmojiMaps();
        private readonly Configuration config;
        private readonly string periodDuration;

        public EventsFormatter(Configuration config, MarketplaceConfig marketplaceConfig)
        {
            this.config = config;
            periodDuration = Time.FormatDuration(marketplaceConfig.PeriodDuration);
        }

        public ChainEventMessage[] GetInitializationEvents(Configuration config)
        {
            return [
                FormatBlock(0, "Bot initializing...",
                    $"History-check start (UTC) = {Time.FormatTimestamp(config.HistoryStartUtc)}",
                    $"Update interval = {Time.FormatDuration(config.Interval)}"
                )
            ];
        }

        public ChainEventMessage[] GetEvents()
        {
            var result = events.ToArray();
            events.Clear();
            return result;
        }

        public string[] GetErrors()
        {
            var result = errors.ToArray();
            errors.Clear();
            return result;
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            var request = requestEvent.Request;
            AddRequestBlock(requestEvent, emojiMaps.NewRequest, "New Request",
                $"Client: {request.Client}",
                $"Content: {BytesToHexString(request.Request.Content.Cid)}",
                $"Duration: {BigIntToDuration(request.Request.Ask.Duration)}",
                $"Expiry: {BigIntToDuration(request.Request.Expiry)}",
                $"CollateralPerByte: {BitIntToTestTokens(request.Request.Ask.CollateralPerByte)}",
                $"PricePerBytePerSecond: {BitIntToTestTokens(request.Request.Ask.PricePerBytePerSecond)}",
                $"Number of Slots: {request.Request.Ask.Slots}",
                $"Slot Tolerance: {request.Request.Ask.MaxSlotLoss}",
                $"Slot Size: {BigIntToByteSize(request.Request.Ask.SlotSize)}",
                $"Proof Probability: 1 / {request.Request.Ask.ProofProbability} every {periodDuration}"
            );
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, emojiMaps.Cancelled, "Cancelled");
        }

        public void OnRequestFailed(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, emojiMaps.Failed, "Failed");
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, emojiMaps.Finished, "Finished");
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, emojiMaps.Started, "Started");
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex, bool isRepair)
        {
            AddRequestBlock(requestEvent, GetSlotFilledIcon(isRepair), GetSlotFilledTitle(isRepair),
                $"Host: {host}",
                $"Slot Index: {slotIndex}"
            );
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            AddRequestBlock(requestEvent, emojiMaps.SlotFreed, "Slot Freed",
                $"Slot Index: {slotIndex}"
            );
        }

        public void OnSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            AddRequestBlock(requestEvent, emojiMaps.SlotReservationsFull, "Slot Reservations Full",
                $"Slot Index: {slotIndex}"
            );
        }

        public void OnProofSubmitted(BlockTimeEntry block, string id)
        {
            // There are a lot of these.
        }

        public void OnError(string msg)
        {
            errors.Add(msg);
        }

        public void OnMissedSlots(ProofPeriod period, List<MissedSlot> slots)
        {
            var isMany = slots.Count > 10;
            var lines = new List<string> {
                $"In period {period.PeriodNumber}: ({period.TimeRange})"
            };

            if (isMany)
            {
                lines.Add($"{slots.Count} storage proofs were missed! {emojiMaps.ManyProofsMissed}");
                lines.Add("(number of missed proofs > 10: Details omitted from this message.)");
            }
            else
            {
                lines.Add($"{slots.Count} storage proofs were missed.");
                foreach (var s in slots)
                {
                    var host = s.SlotReport.Host.AsStr();
                    var request = FormatRequestId(s.Request);
                    var idx = s.SlotReport.Index;
                    var marked = s.SlotReport.MarkedAsMissing;

                    lines.Add($" - '{host}' missed a proof for request {request} (slotIndex:{idx}, marked:{marked})");
                }
            }

            AddBlock(0, $"{emojiMaps.ProofReport} **Proof system report**", lines.ToArray());
        }

        private string GetSlotFilledIcon(bool isRepair)
        {
            if (isRepair) return emojiMaps.SlotRepaired;
            return emojiMaps.SlotFilled;
        }

        private string GetSlotFilledTitle(bool isRepair)
        {
            if (isRepair) return $"Slot Repaired";
            return $"Slot Filled";
        }

        private void AddRequestBlock(RequestEvent requestEvent, string icon, string eventName, params string[] content)
        {
            var blockNumber = $"[{requestEvent.Block.BlockNumber} {FormatDateTime(requestEvent.Block.Utc)}]";
            var title = $"{blockNumber} {icon} **{eventName}** {FormatRequestId(requestEvent)}";
            AddBlock(requestEvent.Block.BlockNumber, title, content);
        }

        private void AddBlock(ulong blockNumber, string title, params string[] content)
        {
            events.Add(FormatBlock(blockNumber, title, content));
        }

        private ChainEventMessage FormatBlock(ulong blockNumber, string title, params string[] content)
        {
            var msg = FormatBlockMessage(title, content);
            return new ChainEventMessage
            {
                BlockNumber = blockNumber,
                Message = msg
            };
        }

        private string FormatBlockMessage(string title, string[] content)
        {
            if (content == null || !content.Any())
            {
                return $"{title}{nl}{nl}";
            }

            return string.Join(nl,
                new string[]
                {
                    title,
                    "```"
                }
                .Concat(content)
                .Concat(new string[]
                {
                    "```"
                })
            ) + nl + nl;
        }

        private string FormatDateTime(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);
        }

        private string FormatRequestId(RequestEvent requestEvent)
        {
            return FormatRequestId(requestEvent.Request);
        }

        private string FormatRequestId(IChainStateRequest request)
        {
            return FormatRequestId(request.RequestId);
        }

        private string FormatRequestId(byte[] id)
        {
            var str = id.ToHex();
            return
                $"({emojiMaps.StringToEmojis(str, 3)})" +
                $"`{str}`";
        }

        private string BytesToHexString(byte[] bytes)
        {
            // libp2p CIDs use MultiBase btcbase64 encoding, which is prefixed with 'z'.
            return "z" + Base58.Encode(bytes);
        }

        private string BigIntToDuration(BigInteger big)
        {
            var span = TimeSpan.FromSeconds((int)big);
            return Time.FormatDuration(span);
        }

        private string BigIntToByteSize(BigInteger big)
        {
            var size = new ByteSize((long)big);
            return size.ToString();
        }

        private string BitIntToTestTokens(BigInteger big)
        {
            var tt = new TestToken(big);
            return tt.ToString();
        }
    }
}
