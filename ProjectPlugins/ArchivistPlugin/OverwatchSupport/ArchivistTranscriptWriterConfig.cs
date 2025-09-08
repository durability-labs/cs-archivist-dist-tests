namespace ArchivistPlugin.OverwatchSupport
{
    public class ArchivistTranscriptWriterConfig
    {
        public ArchivistTranscriptWriterConfig(string outputPath, bool includeBlockReceivedEvents)
        {
            OutputPath = outputPath;
            IncludeBlockReceivedEvents = includeBlockReceivedEvents;
        }

        public string OutputPath { get; }
        public bool IncludeBlockReceivedEvents { get; }
    }
}
