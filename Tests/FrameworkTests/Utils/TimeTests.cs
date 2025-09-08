using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class TimeTests
    {
        [Test]
        public void Timespan()
        {
            Assert.That(Time.ParseTimespan("10"), Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(Time.ParseTimespan("10s"), Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(Time.ParseTimespan("10m"), Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(Time.ParseTimespan("10d"), Is.EqualTo(TimeSpan.FromDays(10)));
            Assert.That(Time.ParseTimespan("120s"), Is.EqualTo(TimeSpan.FromSeconds(120)));
            Assert.That(Time.ParseTimespan("2d14h6m28s"), Is.EqualTo(
                TimeSpan.FromDays(2) +
                TimeSpan.FromHours(14) +
                TimeSpan.FromMinutes(6) +
                TimeSpan.FromSeconds(28)
            ));
        }

        [Test]
        public void Averaging()
        {
            var first = RollingAverage.GetNewAverage(0.0f, 1, 1.0f);
            Assert.That(first, Is.EqualTo(1.0f));

            var fifth = RollingAverage.GetNewAverage(5.0f, 5, 0.0f);
            var expected = new[] { 5.0f, 5.0f, 5.0f, 5.0f, 0.0f }.Average();
            Assert.That(fifth, Is.EqualTo(expected));
        }
    }
}
