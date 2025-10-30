﻿using ArchivistClient;
using ArchivistReleaseTests.Utils;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class ProofWorkloadTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 1;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();

        [Test]
        [Combinatorial]
        public void ProofWorkload(
            [Rerun] int rerun,
            [Values(12)] int secondsPerProof
        )
        {
            var assumeProofTime = TimeSpan.FromSeconds(secondsPerProof);
            Log($"Assume proof calculation + submit takes: {Time.FormatDuration(assumeProofTime)}");

            var periodDuration = GetContracts().Deployment.Config.PeriodDuration;
            Log($"Proving period: {Time.FormatDuration(periodDuration)}");

            var maxProofsPerPeriod = Convert.ToInt32(Math.Floor(periodDuration / assumeProofTime)) - 1;
            Log($"Max number of proofs one host can reasonably calculate and submit during one period: {maxProofsPerPeriod}");

            StartHosts();
            StartValidator();
            var client = StartClients().Single();

            var request = client.Marketplace.RequestStorage(
                new StoragePurchaseRequest(client.UploadFile(GenerateTestFile(maxProofsPerPeriod.MB())))
                {
                    ProofProbability = 1,
                    MinRequiredNumberOfNodes = (uint)maxProofsPerPeriod,
                    NodeFailureTolerance = 1
                });

            request.WaitForStorageContractFinished();
        }
    }
}
