using ArchivistClient;

namespace ArchivistPlugin
{
    public interface IArchivistStarter
    {
        IArchivistInstance[] BringOnline(ArchivistSetup archivistSetup);
        void Decommission();
    }
}
