using ArchivistClient;

namespace ArchivistPlugin
{
    public class ProcessControlMap : IProcessControlFactory
    {
        private readonly Dictionary<string, IProcessControl> processControlMap = new Dictionary<string, IProcessControl>();

        public void Add(IArchivistInstance instance, IProcessControl control)
        {
            processControlMap.Add(instance.Name, control);
        }

        public void Remove(IArchivistInstance instance)
        {
            processControlMap.Remove(instance.Name);
        }

        public IProcessControl CreateProcessControl(IArchivistInstance instance)
        {
            return Get(instance);
        }

        public IProcessControl Get(IArchivistInstance instance)
        {
            return processControlMap[instance.Name];
        }

        public void StopAll()
        {
            var pcs = processControlMap.Values.ToArray();
            processControlMap.Clear();

            foreach (var c in pcs) c.Stop(waitTillStopped: true);
        }
    }
}
