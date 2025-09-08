using ArchivistClient;
using ArchivistPlugin.OverwatchSupport.LineConverters;
using Logging;
using OverwatchTranscript;
using Utils;

namespace ArchivistPlugin.OverwatchSupport
{
    public class ArchivistLogConverter
    {
        private readonly ITranscriptWriter writer;
        private readonly ArchivistTranscriptWriterConfig config;
        private readonly IdentityMap identityMap;

        public ArchivistLogConverter(ITranscriptWriter writer, ArchivistTranscriptWriterConfig config, IdentityMap identityMap)
        {
            this.writer = writer;
            this.config = config;
            this.identityMap = identityMap;
        }

        public void ProcessLog(IDownloadedLog log)
        {
            var name = DetermineName(log);
            var identityIndex = identityMap.GetIndex(name);
            var runner = new ConversionRunner(writer, config, identityMap, identityIndex);
            runner.Run(log);
        }

        private string DetermineName(IDownloadedLog log)
        {
            // Expected string:
            // Downloading container log for '<Downloader1>'
            var nameLine = log.FindLinesThatContain("Downloading container log for").First();
            return Str.Between(nameLine, "'", "'");
        }
    }

    public class ConversionRunner
    {
        private readonly ITranscriptWriter writer;
        private readonly IdentityMap nameIdMap;
        private readonly int nodeIdentityIndex;
        private readonly ILineConverter[] converters;

        public ConversionRunner(ITranscriptWriter writer, ArchivistTranscriptWriterConfig config, IdentityMap nameIdMap, int nodeIdentityIndex)
        {
            this.nodeIdentityIndex = nodeIdentityIndex;
            this.writer = writer;
            this.nameIdMap = nameIdMap;

            converters = CreateConverters(config).ToArray();
        }

        private IEnumerable<ILineConverter> CreateConverters(ArchivistTranscriptWriterConfig config)
        {
            if (config.IncludeBlockReceivedEvents)
            {
                yield return new BlockReceivedLineConverter();
            }
            yield return new BootstrapLineConverter();
            yield return new DialSuccessfulLineConverter();
            yield return new PeerDroppedLineConverter();
        }

        public void Run(IDownloadedLog log)
        {
            log.IterateLines(line =>
            {
                foreach (var converter in converters)
                {
                    ProcessLine(line, converter);
                }
            });
        }

        private void AddEvent(DateTime utc, Action<OverwatchArchivistEvent> action)
        {
            var e = new OverwatchArchivistEvent
            {
                NodeIdentity = nodeIdentityIndex,
            };
            action(e);

            e.Write(utc, writer);
        }

        private void ProcessLine(string line, ILineConverter converter)
        {
            if (!line.Contains(converter.Interest)) return;

            var archivistLine = ArchivistLogLine.Parse(line);

            if (archivistLine == null) throw new Exception("Unable to parse required line");
            EnsureFullIds(archivistLine);

            converter.Process(archivistLine, (action) =>
            {
                AddEvent(archivistLine.TimestampUtc, action);
            });
        }

        private void EnsureFullIds(ArchivistLogLine archivistLine)
        {
            // The issue is: node IDs occure both in full and short version.
            // Downstream tools will assume that a node ID string-equals its own ID.
            // So we replace all shortened IDs we can find with their full ones.

            // Usually, the shortID appears as the entire string of an attribute:
            // "peerId=123*567890"
            // But sometimes, it is part of a larger string:
            // "thing=abc:123*567890,def"
            
            foreach (var pair in archivistLine.Attributes)
            {
                if (pair.Value.Contains("*"))
                {
                    archivistLine.Attributes[pair.Key] = nameIdMap.ReplaceShortIds(pair.Value);
                }
            }
        }
    }

    public interface ILineConverter
    {
        string Interest { get; }
        void Process(ArchivistLogLine line, Action<Action<OverwatchArchivistEvent>> addEvent);
    }
}
