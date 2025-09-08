//using DistTestCore;
//using DistTestCore.Archivist;
//using Newtonsoft.Json;
//using NUnit.Framework;
//using Utils;

//namespace ContinuousTests.Tests
//{
//    public class MarketplaceTest : ContinuousTest
//    {
//        public override int RequiredNumberOfNodes => 1;
//        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(10);
//        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
//        public override int EthereumAccountIndex => 200;
//        public override string CustomK8sNamespace => "archivist-continuous-marketplace";

//        private readonly uint numberOfSlots = 3;
//        private readonly ByteSize fileSize = 10.MB();
//        private readonly TestToken pricePerSlotPerSecond = 10.TestTokens();

//        private TestFile file = null!;
//        private ContentId? cid;
//        private string purchaseId = string.Empty;

//        [TestMoment(t: Zero)]
//        public void NodePostsStorageRequest()
//        {
//            var contractDuration = TimeSpan.FromMinutes(8);
//            decimal totalDurationSeconds = Convert.ToDecimal(contractDuration.TotalSeconds);
//            var expectedTotalCost = numberOfSlots * pricePerSlotPerSecond.Amount * (totalDurationSeconds + 1) * 1000000;

//            file = FileManager.GenerateTestFile(fileSize);

//            NodeRunner.RunNode((archivistAccess, marketplaceAccess) =>
//            {
//                cid = UploadFile(archivistAccess.Node, file);
//                Assert.That(cid, Is.Not.Null);

//                purchaseId = marketplaceAccess.RequestStorage(
//                    contentId: cid!,
//                    pricePerSlotPerSecond: pricePerSlotPerSecond,
//                    requiredCollateral: 100.TestTokens(),
//                    minRequiredNumberOfNodes: numberOfSlots,
//                    proofProbability: 10,
//                    duration: contractDuration);

//                Assert.That(!string.IsNullOrEmpty(purchaseId));

//                WaitForContractToStart(archivistAccess, purchaseId);
//            });
//        }

//        [TestMoment(t: MinuteFive + MinuteOne)]
//        public void StoredDataIsAvailableAfterThreeDays()
//        {
//            NodeRunner.RunNode((archivistAccess, marketplaceAccess) =>
//            {
//                var result = DownloadFile(archivistAccess.Node, cid!);

//                file.AssertIsEqual(result);
//            });
//        }
//    }
//}
