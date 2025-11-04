using BlockchainUtils;
using ArchivistClient;
using ArchivistContractsPlugin;
using ArchivistPlugin;
using ArchivistPlugin.OverwatchSupport;
using ArchivistTests.Helpers;
using Core;
using DistTestCore;
using DistTestCore.Helpers;
using DistTestCore.Logs;
using GethPlugin;
using Logging;
using MetricsPlugin;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using OverwatchTranscript;
using Utils;

namespace ArchivistTests
{
    public class ArchivistDistTest : DistTest
    {
        private readonly BlockCache blockCache = new BlockCache(new NullLog());
        private readonly List<IArchivistNode> nodes = new List<IArchivistNode>();
        private ArchivistTranscriptWriter? writer;

        public ArchivistDistTest()
        {
            ProjectPlugin.Load<ArchivistPlugin.ArchivistPlugin>();
            ProjectPlugin.Load<ArchivistContractsPlugin.ArchivistContractsPlugin>();
            ProjectPlugin.Load<GethPlugin.GethPlugin>();
            ProjectPlugin.Load<MetricsPlugin.MetricsPlugin>();
        }

        [SetUp]
        public void SetupArchivistDistTest()
        {
            writer = SetupTranscript();
        }

        [TearDown]
        public void TearDownArchivistDistTest()
        {
            TeardownTranscript();
        }

        protected override void Initialize(FixtureLog fixtureLog)
        {
            Ci.AddArchivistHooksProvider(new ArchivistLogTrackerProvider(nodes.Add));
        }

        public IArchivistNode StartArchivist()
        {
            return StartArchivist(s => { });
        }

        public IArchivistNode StartArchivist(Action<IArchivistSetup> setup)
        {
            return StartArchivist(1, setup)[0];
        }

        public IArchivistNodeGroup StartArchivist(int numberOfNodes)
        {
            return StartArchivist(numberOfNodes, s => { });
        }

        public IArchivistNodeGroup StartArchivist(int numberOfNodes, Action<IArchivistSetup> setup)
        {
            var group = Ci.StartArchivistNodes(numberOfNodes, s =>
            {
                setup(s);
                OnArchivistSetup(s);
            });

            return group;
        }

        public IGethNode StartGethNode(Action<IGethSetup> setup)
        {
            return Ci.StartGethNode(blockCache, setup);
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers(TimeSpan downloadTimeout)
        {
            return new PeerDownloadTestHelpers(GetTestLog(), GetFileManager(), downloadTimeout);
        }

        public void AssertBalance(IArchivistContracts contracts, IArchivistNode archivistNode, Constraint constraint, string msg)
        {
            Assert.Fail("Depricated, use MarketplaceAutobootstrapDistTest assertBalances instead.");
            AssertHelpers.RetryAssert(constraint, () => contracts.GetTestTokenBalance(archivistNode), nameof(AssertBalance) + msg);
        }

        public void CheckLogForErrors(params IArchivistNode[] nodes)
        {
            foreach (var node in nodes) CheckLogForErrors(node);
        }

        public void CheckLogForErrors(IArchivistNode node)
        {
            Log($"Checking {node.GetName()} log for errors.");
            var log = node.DownloadLog();

            log.AssertLogDoesNotContain("Block validation failed");
            log.AssertLogDoesNotContainLinesStartingWith("ERR ");
        }

        public void LogNodeStatus(IArchivistNode node, IMetricsAccess? metrics = null)
        {
            Log("Status for " + node.GetName() + Environment.NewLine +
                GetBasicNodeStatus(node));
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, IArchivistNodeGroup nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, List<IArchivistNode> nodes)
        {
            WaitAndCheckNodesStaysAlive(duration, nodes.ToArray());
        }

        public void WaitAndCheckNodesStaysAlive(TimeSpan duration, params IArchivistNode[] nodes)
        {
            WaitAndCheck(nameof(WaitAndCheckNodesStaysAlive),
                duration,
                loopTime: TimeSpan.FromSeconds(3.0),
                check: () =>
                {
                    foreach (var node in nodes)
                    {
                        Assert.That(node.HasCrashed(), Is.False);

                        var info = node.GetDebugInfo();
                        Assert.That(!string.IsNullOrEmpty(info.Id));
                    }
                });
        }

        public void WaitAndCheck(string name, TimeSpan duration, TimeSpan loopTime, Action check)
        {
            Log($"{name}: {Time.FormatDuration(duration)}...");

            Assert.That(duration.TotalSeconds, Is.GreaterThan(loopTime.TotalSeconds));

            var start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start) < duration)
            {
                Thread.Sleep(loopTime);
                check();
            }

            Log($"{name}: OK");
        }

        public void AssertNodesContainFile(ContentId cid, IArchivistNodeGroup nodes)
        {
            AssertNodesContainFile(cid, nodes.ToArray());
        }

        public void AssertNodesContainFile(ContentId cid, params IArchivistNode[] nodes)
        {
            Log($"{nodes.Names()} {cid}...");

            foreach (var node in nodes)
            {
                var localDatasets = node.LocalFiles();
                CollectionAssert.Contains(localDatasets.Content.Select(c => c.Cid), cid);
            }

            Log("OK");
        }

        private string GetBasicNodeStatus(IArchivistNode node)
        {
            return JsonConvert.SerializeObject(node.GetDebugInfo(), Formatting.Indented) + Environment.NewLine +
                node.Space().ToString() + Environment.NewLine;
        }

        protected virtual void OnArchivistSetup(IArchivistSetup setup)
        {
        }

        private CreateTranscriptAttribute? GetTranscriptAttributeOfCurrentTest()
        {
            var attrs = GetCurrentTestMethodAttribute<CreateTranscriptAttribute>();
            if (attrs.Any()) return attrs.Single();
            return null;
        }

        private ArchivistTranscriptWriter? SetupTranscript()
        {
            var attr = GetTranscriptAttributeOfCurrentTest();
            if (attr == null) return null;

            var config = new ArchivistTranscriptWriterConfig(
                attr.OutputFilename,
                attr.IncludeBlockReceivedEvents
            );

            var log = new LogPrefixer(GetTestLog(), "(Transcript) ");
            var writer = new ArchivistTranscriptWriter(log, config, Transcript.NewWriter(log));
            Ci.AddArchivistHooksProvider(writer);
            return writer;
        }

        private void TeardownTranscript()
        {
            if (writer == null) return;

            var result = GetTestResult();
            var log = GetTestLog();
            writer.AddResult(result.Success, result.Result);
            try
            {
                Stopwatch.Measure(log, "Transcript.ProcessLogs", () =>
                {
                    writer.ProcessLogs(DownloadAllLogs());
                });

                Stopwatch.Measure(log, $"Transcript.FinalizeWriter", () =>
                {
                    writer.IncludeFile(log.GetFullName() + ".log");
                    writer.FinalizeWriter();
                });
            }
            catch (Exception ex)
            {
                log.Error("Failure during transcript teardown: " + ex);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CreateTranscriptAttribute : PropertyAttribute
    {
        public CreateTranscriptAttribute(string outputFilename, bool includeBlockReceivedEvents = true)
        {
            OutputFilename = outputFilename;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputFilename { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
