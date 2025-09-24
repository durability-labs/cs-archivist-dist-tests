using Logging;
using Utils;

namespace BlockchainUtils
{
    public class BlockTimeFinder
    {
        private readonly BlockchainBounds bounds;
        private readonly IWeb3Blocks web3;
        private readonly ILog log;

        public BlockTimeFinder(IWeb3Blocks web3, ILog log)
        {
            this.web3 = web3;
            this.log = log;

            bounds = new BlockchainBounds(log, web3);
            bounds.InitializeBounds();
        }

        public ulong? GetHighestBlockNumberBefore(DateTime moment)
        {
            if (moment < bounds.Earliest.Utc) return null;
            if (moment == bounds.Earliest.Utc) return bounds.Earliest.BlockNumber;
            if (moment >= bounds.Current.Utc) return bounds.Current.BlockNumber;

            return Log(() => Search(bounds.Earliest, bounds.Current, moment, HighestBeforeSelector));
        }

        public ulong? GetLowestBlockNumberAfter(DateTime moment)
        {
            if (moment > bounds.Current.Utc) return null;
            if (moment == bounds.Current.Utc) return bounds.Current.BlockNumber;
            if (moment <= bounds.Earliest.Utc) return bounds.Earliest.BlockNumber;

            return Log(()=> Search(bounds.Earliest, bounds.Current, moment, LowestAfterSelector)); ;
        }

        private ulong Log(Func<ulong> operation)
        {
            var sw = Stopwatch.Begin(log, nameof(BlockTimeFinder), true);
            var result = operation();
            sw.End($"(Bounds: [{bounds.Earliest.BlockNumber}-{bounds.Current.BlockNumber}]");

            return result;
        }

        private ulong Search(BlockTimeEntry lower, BlockTimeEntry upper, DateTime target, Func<DateTime, BlockTimeEntry, bool> isWhatIwant)
        {
            log.Debug($"Search(lower:{lower}, upper:{upper}, target:{Time.ToUnixTimeSeconds(target)})");

            var middle = GetMiddle(lower, upper);
            if (middle.BlockNumber == lower.BlockNumber)
            {
                if (isWhatIwant(target, upper))
                {
                    return upper.BlockNumber;
                }
            }

            if (isWhatIwant(target, middle))
            {
                return middle.BlockNumber;
            }

            if (middle.Utc > target)
            {
                return Search(lower, middle, target, isWhatIwant);
            }
            else
            {
                return Search(middle, upper, target, isWhatIwant);
            }
        }

        private BlockTimeEntry GetMiddle(BlockTimeEntry lower, BlockTimeEntry upper)
        {
            ulong range = upper.BlockNumber - lower.BlockNumber;
            ulong number = lower.BlockNumber + range / 2;
            return GetBlock(number);
        }

        private bool HighestBeforeSelector(DateTime target, BlockTimeEntry entry)
        {
            var next = GetBlock(entry.BlockNumber + 1);
            var t = Flat(target);

            if (Flat(entry.Utc) == Flat(next.Utc) &&
                Flat(entry.Utc) == t)
            {
                return true;
            }

            return
                Flat(entry.Utc) <= t &&
                Flat(next.Utc) > t;
        }

        private bool LowestAfterSelector(DateTime target, BlockTimeEntry entry)
        {
            var previous = GetBlock(entry.BlockNumber - 1);
            var t = Flat(target);

            if (Flat(entry.Utc) == Flat(previous.Utc) &&
                Flat(entry.Utc) == t)
            {
                return true;
            }

            return
                Flat(entry.Utc) >= t &&
                Flat(previous.Utc) < t;
        }

        private long Flat(DateTime utc)
        {
            return Time.ToUnixTimeSeconds(utc);
        }

        private BlockTimeEntry GetBlock(ulong number, bool retry = false)
        {
            var entry = web3.GetTimestampForBlock(number);
            if (entry == null) throw new Exception("Failed to get dateTime for block.");
            return entry;
        }
    }
}
