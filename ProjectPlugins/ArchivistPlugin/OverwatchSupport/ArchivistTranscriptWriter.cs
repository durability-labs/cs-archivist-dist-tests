using ArchivistClient.Hooks;
using Logging;
using OverwatchTranscript;
using Utils;

namespace ArchivistPlugin.OverwatchSupport
{
    public class ArchivistTranscriptWriter : IArchivistHooksProvider
    {
        private const string ArchivistHeaderKey = "cdx_h";
        private readonly ILog log;
        private readonly ArchivistTranscriptWriterConfig config;
        private readonly ITranscriptWriter writer;
        private readonly ArchivistLogConverter converter;
        private readonly IdentityMap identityMap = new IdentityMap();
        private readonly KademliaPositionFinder positionFinder = new KademliaPositionFinder();

        public ArchivistTranscriptWriter(ILog log, ArchivistTranscriptWriterConfig config, ITranscriptWriter transcriptWriter)
        {
            this.log = log;
            this.config = config;
            writer = transcriptWriter;
            converter = new ArchivistLogConverter(writer, config, identityMap);
        }

        public void FinalizeWriter()
        {
            log.Log("Finalizing Archivist transcript...");

            writer.AddHeader(ArchivistHeaderKey, CreateArchivistHeader());
            writer.Write(GetOutputFullPath());

            log.Log("Done");
        }

        private string GetOutputFullPath()
        {
            var outputPath = Path.GetDirectoryName(log.GetFullName());
            if (outputPath == null) throw new Exception("Logfile path is null");
            var filename = Path.GetFileNameWithoutExtension(log.GetFullName());
            if (string.IsNullOrEmpty(filename)) throw new Exception("Logfile name is null or empty");
            var outputFile = Path.Combine(outputPath, filename + "_" + config.OutputPath);
            if (!outputFile.EndsWith(".owts")) outputFile += ".owts";
            return outputFile;
        }

        public IArchivistNodeHooks CreateHooks(string nodeName)
        {
            nodeName = Str.Between(nodeName, "'", "'");
            return new ArchivistNodeTranscriptWriter(writer, identityMap, nodeName);
        }

        public void IncludeFile(string filepath)
        {
            writer.IncludeArtifact(filepath);   
        }

        public void ProcessLogs(IDownloadedLog[] downloadedLogs)
        {
            foreach (var l in downloadedLogs)
            {
                log.Log("Include artifact: " + l.GetFilepath());
                writer.IncludeArtifact(l.GetFilepath());

                // Not all of these logs are necessarily Archivist logs.
                // Check, and process only the Archivist ones.
                if (IsArchivistLog(l))
                {
                    log.Log("Processing Archivist log: " + l.GetFilepath());
                    converter.ProcessLog(l);
                }
            }
        }

        public void AddResult(bool success, string result)
        {
            writer.Add(DateTime.UtcNow, new OverwatchArchivistEvent
            {
                NodeIdentity = -1,
                ScenarioFinished = new ScenarioFinishedEvent
                {
                    Success = success,
                    Result = result
                }
            });
        }

        private OverwatchArchivistHeader CreateArchivistHeader()
        {
            return new OverwatchArchivistHeader
            {
                Nodes = positionFinder.DeterminePositions(identityMap.Get())
            };
        }

        private bool IsArchivistLog(IDownloadedLog log)
        {
            return log.GetLinesContaining("Run Archivist node").Any();
        }
    }
}
