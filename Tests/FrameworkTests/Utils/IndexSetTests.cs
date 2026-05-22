using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class IndexSetTests
    {
        private IndexSet indexSet = new IndexSet();

        [SetUp]
        public void Setup()
        {
            indexSet = new IndexSet();
        }

        [Test]
        [Combinatorial]
        public void CanSetAndUnsetIndices(
            [Values(0, 1, 55, 888, 3333)] int index,
            [Values(true, false)] bool isSet)
        {
            indexSet[index] = isSet;

            Assert.That(indexSet[index - 1], Is.False);
            Assert.That(indexSet[index], Is.EqualTo(isSet));
            Assert.That(indexSet[index + 1], Is.False);
        }

        [Test]
        public void InitializesToZeroLength()
        {
            Assert.That(indexSet.Length, Is.EqualTo(0));
        }

        [Test]
        [Combinatorial]
        public void FirstIndexIsLengthOne(
            [Values(true, false)] bool isSet)
        {
            indexSet[0] = isSet;

            Assert.That(indexSet.Length, Is.EqualTo(1));
        }

        [Test]
        [Combinatorial]
        public void HighestIndexDeterminesLength(
            [Values(3, 8, 123)] int index,
            [Values(true, false)] bool isSet)
        {
            indexSet[index] = isSet;

            Assert.That(indexSet.Length, Is.EqualTo(1 + index));
        }

        [Test]
        public void LengthCorrectWhenCreatedFromEncodedSet()
        {
            var rle = new int[]{
                2, 4, // 2, 3, 4, 5
                7, 1, // 7
                9, 2  // 9, 10
            };

            var set = IndexSet.FromRunLengthEncoded(rle);

            Assert.That(set.Length, Is.EqualTo(11));
        }

        [Test]
        public void Includes()
        {
            var setA = new IndexSet([0, 1, 2, 3, 4, 5, 6, 7]);
            var setB = new IndexSet([2, 3, 5]);

            Assert.That(setA.Includes(setB), Is.True);
            Assert.That(setB.Includes(setA), Is.False);
        }
    }
}
