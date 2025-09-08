using ArchivistClient;

namespace ArchivistPlugin.OverwatchSupport.LineConverters
{
    public class DialSuccessfulLineConverter : ILineConverter
    {
        public string Interest => "Dial successful";

        public void Process(ArchivistLogLine line, Action<Action<OverwatchArchivistEvent>> addEvent)
        {
            var peerId = line.Attributes["peerId"];

            addEvent(e =>
            {
                e.DialSuccessful = new PeerDialSuccessfulEvent
                {
                    TargetPeerId = peerId
                };
            });
        }
    }
}
