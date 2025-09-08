using ArchivistClient;

namespace ArchivistPlugin.OverwatchSupport.LineConverters
{
    public class PeerDroppedLineConverter : ILineConverter
    {
        public string Interest => "Dropping peer";

        public void Process(ArchivistLogLine line, Action<Action<OverwatchArchivistEvent>> addEvent)
        {
            var peerId = line.Attributes["peer"];

            addEvent(e =>
            {
                e.PeerDropped = new PeerDroppedEvent
                {
                    DroppedPeerId = peerId
                };
            });
        }
    }
}
