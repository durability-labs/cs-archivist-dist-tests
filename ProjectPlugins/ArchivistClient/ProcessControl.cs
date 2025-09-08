using Logging;

namespace ArchivistClient
{
    public interface IProcessControlFactory
    {
        IProcessControl CreateProcessControl(IArchivistInstance instance);
    }

    public interface IProcessControl
    {
        void Stop(bool waitTillStopped);
        IDownloadedLog DownloadLog(LogFile file);
        void DeleteDataDirFolder();
        bool HasCrashed();
    }

    public class DoNothingProcessControlFactory : IProcessControlFactory
    {
        public IProcessControl CreateProcessControl(IArchivistInstance instance)
        {
            return new DoNothingProcessControl();
        }
    }

    public class DoNothingProcessControl : IProcessControl
    {
        public void DeleteDataDirFolder()
        {
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            throw new NotImplementedException("Not supported by DoNothingProcessControl");
        }

        public bool HasCrashed()
        {
            return false;
        }

        public void Stop(bool waitTillStopped)
        {
        }
    }
}
