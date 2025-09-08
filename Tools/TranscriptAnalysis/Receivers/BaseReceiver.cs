using ArchivistPlugin.OverwatchSupport;
using Logging;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public abstract class BaseReceiver<T> : IEventReceiver<T>
    {
        protected ILog log { get; private set; } = new NullLog();
        protected OverwatchArchivistHeader Header { get; private set; } = null!;
        protected CsvWriter CsvWriter { get; private set; }
        protected string SourceFilename { get; private set; } = string.Empty;

        public abstract string Name { get; }
        public abstract void Receive(ActivateEvent<T> @event);
        public abstract void Finish();

        protected BaseReceiver()
        {
            CsvWriter = new CsvWriter(log);
        }

        public void Init(string sourceFilename, ILog log, OverwatchArchivistHeader header)
        {
            this.log = new LogPrefixer(log, $"({Name}) ");
            Header = header;
            SourceFilename = sourceFilename;
        }

        protected string? GetPeerId(int nodeIndex)
        {
            return GetIdentity(nodeIndex)?.PeerId;
        }

        protected string? GetName(int nodeIndex)
        {
            return GetIdentity(nodeIndex)?.Name;
        }

        protected ArchivistNodeIdentity? GetIdentity(int nodeIndex)
        {
            if (nodeIndex < 0) return null;
            return Header.Nodes[nodeIndex];
        }

        protected void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
