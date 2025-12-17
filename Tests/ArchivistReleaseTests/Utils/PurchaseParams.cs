using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.Utils
{
    public class PurchaseParams
    {
        private readonly ByteSize blockSize = 64.KB();

        public PurchaseParams(
            int nodes,
            int tolerance,
            TimeSpan duration,
            ByteSize uploadFilesize,
            TestToken pricePerByteSecond,
            TestToken collateralPerByte
        )
        {
            Nodes = nodes;
            Tolerance = tolerance;
            Duration = duration;
            UploadFilesize = uploadFilesize;
            PricePerByteSecond = pricePerByteSecond;
            CollateralPerByte = collateralPerByte;
            EncodedDatasetSize = CalculateEncodedDatasetSize();
            SlotSize = CalculateSlotSize();
            CollateralRequiredPerSlot = CalculateCollateralPerSlot();
            PaymentPerSlot = CalculatePaymentPerSlot();

            Assert.That(IsPowerOfTwo(SlotSize));
        }

        public int Nodes { get; }
        public int Tolerance { get; }
        public TimeSpan Duration { get; }
        public ByteSize UploadFilesize { get; }
        public TestToken PricePerByteSecond { get; }
        public TestToken CollateralPerByte { get; }
        public ByteSize EncodedDatasetSize { get; }
        public ByteSize SlotSize { get; }
        public TestToken CollateralRequiredPerSlot { get; }
        public TestToken PaymentPerSlot { get; }

        private ByteSize CalculateSlotSize()
        {
            // encoded dataset is divided over the nodes.
            // then each slot is rounded up to the nearest power-of-two blocks.
            var numBlocks = EncodedDatasetSize.DivUp(blockSize);
            var numSlotBlocks = DivUp(numBlocks, Nodes);

            // Next power of two:
            var numSlotBlocksPow2 = IsOrNextPowerOf2(numSlotBlocks);
            return new ByteSize(blockSize.SizeInBytes * numSlotBlocksPow2);
        }

        private ByteSize CalculateEncodedDatasetSize()
        {
            var numBlocks = UploadFilesize.DivUp(blockSize);

            var ecK = Nodes - Tolerance;
            var ecM = Tolerance;

            // for each K blocks, we generate M parity blocks
            var numParityBlocks = DivUp(numBlocks, ecK) * ecM;
            var totalBlocks = numBlocks + numParityBlocks;

            return new ByteSize(blockSize.SizeInBytes * totalBlocks);
        }

        private TestToken CalculateCollateralPerSlot()
        {
            return CollateralPerByte * SlotSize.SizeInBytes;
        }

        private TestToken CalculatePaymentPerSlot()
        {
            return PricePerByteSecond * SlotSize.SizeInBytes * Convert.ToInt64(Duration.TotalSeconds);
        }

        private int DivUp(int num, int over)
        {
            var result = 0;
            var remain = num;
            while (remain > over)
            {
                remain -= over;
                result++;
            }
            if (remain > 0) result++;
            return result;
        }

        private int IsOrNextPowerOf2(int n)
        {
            if (IsPowerOfTwo(n)) return n;
            var result = 2;
            while (result < n)
            {
                result = result * 2;
            }
            return result;
        }

        private static bool IsPowerOfTwo(ByteSize size)
        {
            var x = size.SizeInBytes;
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        private static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}
