//using DistTestCore;
//using DistTestCore.Archivist;
//using NUnit.Framework;

//namespace ContinuousTests.Tests
//{
//    public class TransientNodeTest : ContinuousTest
//    {
//        public override int RequiredNumberOfNodes => 3;
//        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(1);
//        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
//        public override string CustomK8sNamespace => nameof(TransientNodeTest).ToLowerInvariant();
//        public override int EthereumAccountIndex => 201;

//        private TestFile file = null!;
//        private ContentId cid = null!;

//        private ArchivistAccess UploadBootstapNode { get { return Nodes[0]; } }
//        private ArchivistAccess DownloadBootstapNode { get { return Nodes[1]; } }
//        private ArchivistAccess IntermediateNode { get { return Nodes[2]; } }

//        [TestMoment(t: 0)]
//        public void UploadWithTransientNode()
//        {
//            file = FileManager.GenerateTestFile(10.MB());

//            NodeRunner.RunNode(UploadBootstapNode, (archivistAccess, marketplaceAccess, lifecycle) =>
//            {
//                cid = UploadFile(archivistAccess, file)!;
//                Assert.That(cid, Is.Not.Null);

//                var dlt = Task.Run(() =>
//                {
//                    Thread.Sleep(10000);
//                    lifecycle.DownloadLog(archivistAccess.Container);
//                });

//                var resultFile = DownloadFile(IntermediateNode, cid);
//                dlt.Wait();
//                file.AssertIsEqual(resultFile);
//            });
//        }

//        [TestMoment(t: 30)]
//        public void DownloadWithTransientNode()
//        {
//            NodeRunner.RunNode(DownloadBootstapNode, (archivistAccess, marketplaceAccess, lifecycle) =>
//            {
//                var resultFile = DownloadFile(archivistAccess, cid);
//                file.AssertIsEqual(resultFile);
//            });
//        }
//    }
//}
