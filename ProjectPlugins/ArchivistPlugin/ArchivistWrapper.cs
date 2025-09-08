using ArchivistClient;
using ArchivistClient.Hooks;
using Core;
using Logging;

namespace ArchivistPlugin
{
    public class ArchivistWrapper
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly ArchivistHooksFactory hooksFactory;
        private DebugInfoVersion? versionResponse;

        public ArchivistWrapper(IPluginTools pluginTools, ProcessControlMap processControlMap, ArchivistHooksFactory hooksFactory)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
            this.hooksFactory = hooksFactory;
        }

        public string GetArchivistId()
        {
            if (versionResponse != null) return versionResponse.Version;
            return "unknown";
        }

        public string GetArchivistRevision()
        {
            if (versionResponse != null) return versionResponse.Revision;
            return "unknown";
        }

        public IArchivistNodeGroup WrapArchivistInstances(IArchivistInstance[] instances)
        {
            var archivistNodeFactory = new ArchivistNodeFactory(
                log: pluginTools.GetLog(),
                fileManager: pluginTools.GetFileManager(),
                hooksFactory: hooksFactory,
                httpFactory: pluginTools,
                processControlFactory: processControlMap);

            var group = CreateArchivistGroup(instances, archivistNodeFactory);

            pluginTools.GetLog().Log($"Archivist version: {group.Version}");
            versionResponse = group.Version;

            return group;
        }

        private ArchivistNodeGroup CreateArchivistGroup(IArchivistInstance[] instances, ArchivistNodeFactory archivistNodeFactory)
        {
            var nodes = instances.Select(archivistNodeFactory.CreateArchivistNode).ToArray();
            var group = new ArchivistNodeGroup(pluginTools, nodes);

            try
            {
                Stopwatch.Measure(pluginTools.GetLog(), "EnsureOnline", group.EnsureOnline);
            }
            catch
            {
                ArchivistNodesNotOnline(instances);
                throw;
            }

            return group;
        }

        private void ArchivistNodesNotOnline(IArchivistInstance[] instances)
        {
            pluginTools.GetLog().Log("Archivist nodes failed to start");
            var log = pluginTools.GetLog();
            foreach (var i in instances)
            {
                var pc = processControlMap.Get(i);
                pc.DownloadLog(log.CreateSubfile(i.Name + "_failed_to_start"));
            }
        }
    }
}
