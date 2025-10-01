namespace BlockchainUtils
{
    public interface IBlockLadder
    {
        BlockTimeEntry GetClosestLower(BlockTimeEntry earliest, DateTime target);
        BlockTimeEntry GetClosestUpper(BlockTimeEntry current, DateTime target);
    }

    public class BlockLadder : IBlockLadder
    {
        private readonly Ladder[] ladders = new[]
        {
            new Ladder(TimeSpan.FromMinutes(2), 30), // Covers most recent hour.
            new Ladder(TimeSpan.FromMinutes(10), 48), // Covers most recent 8 hours.
            new Ladder(TimeSpan.FromHours(1), 24) // Covers most recent day.
        };

        public void Add(BlockTimeEntry entry)
        {
            foreach (var l in ladders) l.Add(entry);
        }

        public BlockTimeEntry GetClosestLower(BlockTimeEntry earliest, DateTime target)
        {
            foreach (var l in ladders) earliest = l.GetClosestLower(earliest, target);
            return earliest;
        }

        public BlockTimeEntry GetClosestUpper(BlockTimeEntry current, DateTime target)
        {
            foreach (var l in ladders) current = l.GetClosestUpper(current, target);
            return current;
        }

        public class Ladder
        {
            private readonly TimeSpan gap;
            private readonly int maxLength;
            private readonly List<BlockTimeEntry> steps = new List<BlockTimeEntry>();

            public Ladder(TimeSpan gap, int maxLength)
            {
                this.gap = gap;
                this.maxLength = maxLength;
            }

            public void Add(BlockTimeEntry entry)
            {
                if (steps.Count == 0)
                {
                    steps.Add(entry);
                    return;
                }

                var requiredMinUtc = steps[0].Utc + gap;
                if (entry.Utc < requiredMinUtc) return;
                
                steps.Insert(0, entry);
                if (steps.Count > maxLength)
                {
                    steps.RemoveAt(steps.Count - 1);
                }
            }

            public BlockTimeEntry GetClosestLower(BlockTimeEntry earliest, DateTime target)
            {
                var selected = earliest;
                foreach (var step in steps)
                {
                    if (step.Utc > selected.Utc && step.Utc < target)
                    {
                        selected = step;
                    }
                }
                return selected;
            }

            public BlockTimeEntry GetClosestUpper(BlockTimeEntry current, DateTime target)
            {
                var selected = current;
                foreach (var step in steps)
                {
                    if (step.Utc < selected.Utc && step.Utc > target)
                    {
                        selected = step;
                    }
                }
                return selected;
            }
        }
    }
}
