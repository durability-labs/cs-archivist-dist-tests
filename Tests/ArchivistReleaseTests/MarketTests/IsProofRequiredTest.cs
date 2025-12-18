using ArchivistClient;
using ArchivistReleaseTests.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class IsProofRequiredTest : MarketplaceAutoBootstrapDistTest
    {
        #region Setup

        protected override int NumberOfHosts => 6;
        protected override int NumberOfClients => 1;
        protected override TestToken HostStartingBalance => DefaultPurchase.CollateralRequiredPerSlot * 1.1; // Each host can hold 1 slot.

        #endregion

        /// <summary>
        /// This test is to guarantee that the proving period are correctly configured
        /// in the testing environment. If this test fails, your block frequency
        /// is not compatible with the configuration of your marketplace contracts.
        /// And you can't trust any of the other marketplace-relate tests until this is fixed.
        /// </summary>
        [Test]
        [Combinatorial]
        [Ignore("Used to ensure correct marketplace configuration")]
        public void IsProofRequired(
            [Rerun] int rerun)
        {
            var mins = TimeSpan.FromMinutes(10.0);

            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            var purchase = CreateStorageRequest(client, mins);
            purchase.WaitForStorageContractStarted();

            var requestId = purchase.PurchaseId.HexToByteArray();
            var numSlots = DefaultPurchase.Nodes;
            var map = new Dictionary<ulong, PeriodSlot[]>();

            Log($"Checking IsProofRequired every second for {Time.FormatDuration(mins)}.");
            var endUtc = DateTime.UtcNow + mins;

            var stopping = 0;
            while (DateTime.UtcNow < endUtc)
            {
                if (stopping > 0)
                {
                    stopping--;
                    if (stopping == 0) Assert.Fail("Test failed");
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
                var periodNumber = GetContracts().GetPeriodNumber(DateTime.UtcNow);
                var blockNumber = GetGeth().GetSyncedBlockNumber();

                if (!map.ContainsKey(periodNumber))
                {
                    var n = new PeriodSlot[numSlots];
                    for (var i = 0; i < numSlots; i++) n[i] = new PeriodSlot();
                    map.Add(periodNumber, n);
                }
                var slots = map[periodNumber];

                for (var i = 0; i < numSlots; i++)
                {
                    var slot = slots[i];
                    
                    var isR = GetContracts().IsProofRequired(requestId, i);
                    var willR = GetContracts().WillProofBeRequired(requestId, i);
                    var samePeriodNumber = GetContracts().GetPeriodNumber(DateTime.UtcNow);

                    if (periodNumber != samePeriodNumber)
                    {
                        Log("Period changed during slot checks. Skipping to next.");
                        break;
                    }

                    var doSwap = false;

                    if (isR && slot.WillBeRequired) doSwap = true;
                    if (willR && slot.IsRequired) doSwap = true;

                    if (doSwap)
                    {
                        slot.Swapped++;
                        if (slot.Swapped > 1)
                        {
                            Log($"Index {i} has swapped for the second time this period. Test failed. Starting to stop...");
                            stopping = 80; // This completes the current period + some extra.
                        }
                    }

                    slot.IsRequired = isR;
                    slot.WillBeRequired = willR;

                    if (slot.IsRequired && slot.WillBeRequired)
                    {
                        // This is a race condition. We sampled isRequired just before the pointer looped
                        // and willBeRequired just after.
                        Log("race condition detected and fixed.");
                        slot.IsRequired = false;
                    }
                }

                Log($"[{blockNumber?.ToString().PadLeft(4, '0')}]" +
                    $"{periodNumber.ToString().PadLeft(12, '0')} => Slots: " +
                    $"[{string.Join("    ", slots.Select(s => s.ToString()))}]");
            }
        }

        public class PeriodSlot
        {
            public bool IsRequired { get; set; }
            public bool WillBeRequired { get; set; }
            public int Swapped { get; set; }

            public override string ToString()
            {
                return $"(is:{IsRequired}-willBe:{WillBeRequired}-swap:{Swapped})";
            }
        }

        private IStoragePurchaseContract CreateStorageRequest(IArchivistNode client, TimeSpan minutes)
        {
            var cid = client.UploadFile(GenerateTestFile(DefaultPurchase.UploadFilesize));
            var config = GetContracts().Deployment.Config;
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = minutes * 2.0,
                ProofProbability = 1, // One proof every period. Free slot as quickly as possible.
            });
        }
    }
}
