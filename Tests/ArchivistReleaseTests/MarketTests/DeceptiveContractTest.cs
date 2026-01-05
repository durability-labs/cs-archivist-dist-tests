using ArchivistClient;
using ArchivistContractsPlugin.Marketplace;
using ArchivistPlugin;
using ArchivistReleaseTests.Utils;
using GethPlugin;
using Nethereum.Hex.HexConvertors.Extensions;
using NUnit.Framework;
using Utils;

namespace ArchivistReleaseTests.MarketTests
{
    [TestFixture(5)]
    public class DeceptiveContractTest : MarketplaceAutoBootstrapDistTest
    {
        private readonly PurchaseParams largePurchaseParams;
        private readonly int numberOfDeceptiveRequests;

        public DeceptiveContractTest(int numberOfDeceptiveRequests)
        {
            this.numberOfDeceptiveRequests = numberOfDeceptiveRequests;

            largePurchaseParams = DefaultPurchase.WithUploadFilesize(100.MB());
        }

        protected override int NumberOfHosts => 5;
        protected override int NumberOfClients => 1;
        protected override bool MonitorProofPeriods => false;

        private (Request, IArchivistNode) PrepareLargeDatasetAndRequest()
        {
            Log($"Creating a storage request with {largePurchaseParams.SlotSize} slots, copying the on-chain request and waiting until it expires...");
            var client = StartClients().Single();
            var clientRequest = client.Marketplace.RequestStorage(new StoragePurchaseRequest(
                client.UploadFile(GenerateTestFile(largePurchaseParams.UploadFilesize)))
                {
                    // No host will pick up a request with this collateral requirement:
                    CollateralPerByte = DefaultAvailabilityMaxCollateralPerByte + 10.Tst()

                    // The purpose of this call is to:
                    // A) Get the large dataset encoded/verifiable and available in the client node
                    // 2) Get the request from chain, so we can manipulate it and post malicious copies.
                }
            );

            clientRequest.WaitForStorageContractSubmitted();
            // We break the test code abstraction here to get and post a request on-chain directly.
            var request = Time.Retry(() => GetRawRequest(clientRequest.PurchaseId), nameof(GetRawRequest));
            
            Log($"On-chain request has slotSize {request.Ask.SlotSize} ({new ByteSize(Convert.ToInt64(request.Ask.SlotSize))})");
            return (request, client);
        }

        [Test]
        public void HostQuotaTooSmallForRealSlotSize()
        {
            var (request, client) = PrepareLargeDatasetAndRequest();
            var deceptiveRequestSlotSize = largePurchaseParams.SlotSize.Multiply(0.1);
            var hostQuotaSize = deceptiveRequestSlotSize.Multiply(1.1);

            Assert.That(hostQuotaSize.SizeInBytes, Is.LessThan(largePurchaseParams.SlotSize.SizeInBytes));

            Log($"Starting hosts with quota size: {hostQuotaSize}");
            var hosts = StartHosts(s => s.WithStorageQuota(hostQuotaSize));

            Log($"Creating {numberOfDeceptiveRequests} deceptive request(s) for the same data.");
            Log($"Real slotSize: {largePurchaseParams.SlotSize}");
            Log($"Deceptive request slotSize: {deceptiveRequestSlotSize}");
            Log("The slotSize in the request will trick the host into thinking it can store a full slot.");
            Log("But the real slotSize is too large for the host quota, so it can't. So it should discard this request.");

            request.Ask.SlotSize = Convert.ToUInt64(deceptiveRequestSlotSize.SizeInBytes);
            PostRawRequests(numberOfDeceptiveRequests, request, client.EthAccount);

            AssertHostsIgnoreDeceptiveRequest(hosts);

            CreateAndStartLegitRequest(client);
        }

        [Test]
        public void HostQuotaIsLargeEnoughForRealSlotSizeButDeceptiveRequestIsRejectedAnyway()
        {
            var (request, client) = PrepareLargeDatasetAndRequest();
            var deceptiveRequestSlotSize = largePurchaseParams.SlotSize.Multiply(0.1);
            var hostQuotaSize = largePurchaseParams.SlotSize.Multiply(largePurchaseParams.Nodes * (numberOfDeceptiveRequests + 1));

            Assert.That(hostQuotaSize.SizeInBytes, Is.GreaterThan(largePurchaseParams.SlotSize.SizeInBytes * numberOfDeceptiveRequests));

            Log($"Starting hosts with quota size: {hostQuotaSize}");
            var hosts = StartHosts(s => s.WithStorageQuota(hostQuotaSize));

            Log($"Creating {numberOfDeceptiveRequests} deceptive request(s) for the same data.");
            Log($"Real slotSize: {largePurchaseParams.SlotSize}");
            Log($"Deceptive request slotSize: {deceptiveRequestSlotSize}");
            Log("The slotSize in the request is under-reporting the real slotSize of the dataset.");
            Log("The hosts have enough capacity to fill the slots and service the request, but.");
            Log("They will be paid for the deceptive, small slotSize, while storing and proving");
            Log("the large slotSizes. The hosts should not fall for such trickery! and reject the request.");

            request.Ask.SlotSize = Convert.ToUInt64(deceptiveRequestSlotSize.SizeInBytes);
            PostRawRequests(numberOfDeceptiveRequests, request, client.EthAccount);

            AssertHostsIgnoreDeceptiveRequest(hosts);

            CreateAndStartLegitRequest(client);
        }

        private void AssertHostsIgnoreDeceptiveRequest(IArchivistNodeGroup hosts)
        {
            Log("Hosts will attempt to fill the slots of the deceptive contract. " +
                "When they download the manifest, they should know something's wrong and disregard the request. " +
                "We check that they don't crash for the duration of the request expiry.");

            var defaultExpiry = TimeSpan.FromMinutes(10.0); // extract this from request type.
            WaitAndCheckNodesStaysAlive(defaultExpiry, hosts);

            Log("The hosts should be empty.");
            AssertHostsAreEmpty(hosts);
        }

        private void CreateAndStartLegitRequest(IArchivistNode client)
        {
            Log("Now we create a legit request. The hosts should pick it up and the request should start.");
            var legitRequest = client.Marketplace
                .RequestStorage(new StoragePurchaseRequest(
                    client.UploadFile(GenerateTestFile(DefaultPurchase.UploadFilesize))
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
