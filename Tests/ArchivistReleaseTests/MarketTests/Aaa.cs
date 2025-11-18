using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    public class Aaa : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 1;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();

        [Test]
        public void A()
        {
            StartHosts();
        }
    }
}
