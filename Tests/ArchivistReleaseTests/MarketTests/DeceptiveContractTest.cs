using ArchivistClient;
using ArchivistContractsPlugin.Marketplace;
using ArchivistReleaseTests.Utils;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture]
    public class DeceptiveContractTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly PurchaseParams deceptivePurchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 3.MB()
        );
        // We will be creating a request for a dataset that is much larger than
        // than what we report in our on-chain request.
        private readonly PurchaseParams actualPurchaseParams = new PurchaseParams(
            nodes: 4,
            tolerance: 2,
            uploadFilesize: 100.MB()
        );

        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => deceptivePurchaseParams.SlotSize.Multiply(1.1);
        protected override bool MonitorProofPeriods => false;

        [Test]
        [Combinatorial]
        public void RequestSizeTooSmall(
            [Values(1, 5)] int numberOfDeceptiveRequests)
        {
            var (hosts, clients) = JumpStartHostsAndClients();
            var client = clients.Single();

            var clientRequest = client.Marketplace
                .RequestStorage(new StoragePurchaseRequest(
                    client.UploadFile(GenerateTestFile(actualPurchaseParams.UploadFilesize))
                ));

            Log($"This request has slot-size {actualPurchaseParams.SlotSize}. " +
                $"Each host will have enough availability to fill {HostAvailabilitySize}. " +
                $"Therefore, this request will be ignored.");

            // We break the test code abstraction here to get and post a request on-chain directly.
            var request = Time.Retry(() => GetRawRequest(clientRequest.PurchaseId), nameof(GetRawRequest));
            Assert.That(request.Ask.SlotSize, Is.EqualTo(actualPurchaseParams.SlotSize.SizeInBytes));

            Log($"On-chain request has slot size {request.Ask.SlotSize} ({new ByteSize(Convert.ToInt64(request.Ask.SlotSize))})");
            Log($"Creating {numberOfDeceptiveRequests} deceptive request(s) for the same data with slot size {deceptivePurchaseParams.SlotSize.SizeInBytes} " +
                $"({deceptivePurchaseParams.SlotSize})");

            request.Ask.SlotSize = Convert.ToUInt64(deceptivePurchaseParams.SlotSize.SizeInBytes);
            PostRawRequests(numberOfDeceptiveRequests, request, client.EthAccount);

            Log("Hosts will attempt to fill the slots of the deceptive contract. " +
                "When they download the manifest, they should know something's wrong and disregard the request. " +
                "We check that they don't crash for the next 5 minutes.");

            WaitAndCheckNodesStaysAlive(TimeSpan.FromMinutes(5), hosts);

            AssertHostAvailabilitiesAreEmpty(hosts);

            Log("Now, we create a legit request. The hosts should pick it up and the request should start.");

            var legitRequest = client.Marketplace
                .RequestStorage(new StoragePurchaseRequest(
                    client.UploadFile(GenerateTestFile(deceptivePurchaseParams.UploadFilesize))
                ));

            legitRequest.WaitForStorageContractStarted();
        }

        private void PostRawRequests(int number, Request request, EthAccount clientAccount)
        {
            var geth = GetGeth().WithDifferentAccount(clientAccount);
            var contracts = GetContracts().WithDifferentGeth(geth);
            var marketplaceAddress = contracts.Deployment.MarketplaceAddress;

            contracts.ApproveTestTokens(marketplaceAddress, (StartingBalanceTST / 2).Tst());

            var remaining = number;
            var timeout = DateTime.UtcNow + TimeSpan.FromMinutes(10);
            while (remaining > 0)
            {
                if (DateTime.UtcNow > timeout) Assert.Fail("Failed to post requests within 10 minutes");

                Time.Retry(() =>
                {
                    CreateRequest(geth, marketplaceAddress, request);
                    request.Expiry++; // We must modify the request. Identical requests are ignored by the contract.
                    remaining--;
                }, nameof(CreateRequest));
            }
        }

        private void CreateRequest(IGethNode geth, ContractAddress marketplaceAddress, Request request)
        {
            var func = new RequestStorageFunction
            {
                Request = request
            };

            geth.SendTransaction<RequestStorageFunction>(marketplaceAddress, func);
        }

        private Request GetRawRequest(string purchaseId)
        {
            var func = new GetRequestFunction
            {
                RequestId = purchaseId.HexToByteArray()
            };

            var request = GetGeth().Call<GetRequestFunction, GetRequestOutputDTO>(GetContracts().Deployment.MarketplaceAddress, func);
            return request.ReturnValue1;
        }
    }
}
