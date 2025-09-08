//using DistTestCore;
//using DistTestCore.Archivist;
//using NUnit.Framework;

//namespace ContinuousTests.Tests
//{
//    public class UploadPerformanceTest : PerformanceTest
//    {
//        public override int RequiredNumberOfNodes => 1;

//        [TestMoment(t: Zero)]
//        public void UploadTest()
//        {
//            UploadTest(100, Nodes[0]);
//        }
//    }

//    public class DownloadLocalPerformanceTest : PerformanceTest
//    {
//        public override int RequiredNumberOfNodes => 1;

//        [TestMoment(t: Zero)]
//        public void DownloadTest()
//        {
//            DownloadTest(100, Nodes[0], Nodes[0]);
//        }
//    }

//    public class DownloadRemotePerformanceTest : PerformanceTest
//    {
//        public override int RequiredNumberOfNodes => 2;

//        [TestMoment(t: Zero)]
//        public void DownloadTest()
//        {
//            DownloadTest(100, Nodes[0], Nodes[1]);
//        }
//    }

//    public abstract class PerformanceTest : ContinuousTest
//    {
//        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(10);
//        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

//        public void UploadTest(int megabytes, ArchivistAccess uploadNode)
//        {
//            var file = FileManager.GenerateTestFile(megabytes.MB());

//            var time = Measure(() =>
//            {
//                UploadFile(uploadNode, file);
//            });

//            var timePerMB = time / megabytes;

//            Assert.That(timePerMB, Is.LessThan(ArchivistContainerRecipe.MaxUploadTimePerMegabyte), "MaxUploadTimePerMegabyte performance threshold breached.");
//        }

//        public void DownloadTest(int megabytes, ArchivistAccess uploadNode, ArchivistAccess downloadNode)
//        {
//            var file = FileManager.GenerateTestFile(megabytes.MB());

//            var cid = UploadFile(uploadNode, file);
//            Assert.That(cid, Is.Not.Null);

//            TestFile? result = null;
//            var time = Measure(() =>
//            {
//                result = DownloadFile(downloadNode, cid!);
//            });

//            file.AssertIsEqual(result);

//            var timePerMB = time / megabytes;

//            Assert.That(timePerMB, Is.LessThan(ArchivistContainerRecipe.MaxDownloadTimePerMegabyte), "MaxDownloadTimePerMegabyte performance threshold breached.");
//        }

//        private static TimeSpan Measure(Action action)
//        {
//            var start = DateTime.UtcNow;
//            action();
//            return DateTime.UtcNow - start;
//        }
//    }
//}
