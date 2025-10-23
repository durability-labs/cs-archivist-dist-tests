using System.Globalization;
using ArchivistClient;
using NUnit.Framework;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class LogMerger
    {
        [Test]
        public void Merge()
        {
            var basePath = "d:\\Dev\\cs-archivist-dist-tests\\Tests\\ArchivistReleaseTests\\bin\\Debug\\net8.0\\ArchivistTestLogs\\2025-10\\23\\08-32-59-ArchivistReleaseTests-MarketTests-ProofTest[97]\\";
            var nodeLogPath = $"{basePath}08-32-59Z_Proof[97]_000003_host3.log";
            var testLogPath = $"{basePath}08-32-59Z_Proof[97].log";
            var outputPath = $"{basePath}merged.log";

            var lines = new List<LineStamp>();
            ParseNodeLog(lines, nodeLogPath);
            ParseTestLog(lines, testLogPath);
            WriteOutput(lines, outputPath);
        }

        private void WriteOutput(List<LineStamp> output, string outputPath)
        {
            var sorted = output.OrderBy(e => e.Utc).ToArray();
            File.WriteAllLines(outputPath, sorted.Select(s => s.Line));
        }

        private void ParseTestLog(List<LineStamp> output, string testLogPath)
        {
            var lines = File.ReadAllLines(testLogPath);
            foreach (var l in lines)
            {
                if (DateTime.TryParse(l.Substring(1, 28), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime utc))
                {
                    output.Add(new LineStamp(utc.ToUniversalTime(), l));
                }
            }
        }

        private void ParseNodeLog(List<LineStamp> output, string nodeLogPath)
        {
            var lines = File.ReadAllLines(nodeLogPath);
            foreach (var l in lines)
            {
                var info = ArchivistLogLine.Parse(l);
                if (info != null)
                {
                    output.Add(new LineStamp(info.TimestampUtc, l));
                }
            }
        }

        public class LineStamp
        {
            public LineStamp(DateTime utc, string line)
            {
                Utc = utc;
                Line = line;
            }

            public DateTime Utc { get; }
            public string Line { get; }
        }
    }
}
