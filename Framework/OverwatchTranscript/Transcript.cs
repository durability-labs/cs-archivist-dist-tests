using Logging;

namespace OverwatchTranscript
{
    public static class Transcript
    {
        public static ITranscriptWriter NewWriter(ILog log)
        {
            log = new LogPrefixer(log, "(TranscriptWriter) ");
            return new TranscriptWriter(log, NewWorkDir());
        }

        public static ITranscriptReader NewReader(string transcriptFile)
        {
            return new TranscriptReader(NewWorkDir(), transcriptFile);
        }

        private static string NewWorkDir()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }
    }
}
