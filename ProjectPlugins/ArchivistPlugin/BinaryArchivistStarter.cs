using ArchivistClient;
using Core;
using Utils;
using System.Diagnostics;

namespace ArchivistPlugin
{
    public class BinaryArchivistStarter : IArchivistStarter
    {
        private readonly IPluginTools pluginTools;
        private readonly ProcessControlMap processControlMap;
        private readonly static NumberSource numberSource = new NumberSource(1);
        private readonly static FreePortFinder freePortFinder = new FreePortFinder();
        private readonly static object _lock = new object();
        private readonly static string dataParentDir = "archivist_disttest_datadirs";
        private readonly static ArchivistExePath archivistExePath = new ArchivistExePath();

        static BinaryArchivistStarter()
        {
            StopAllArchivistProcesses();
            DeleteParentDataDir();
        }

        public BinaryArchivistStarter(IPluginTools pluginTools, ProcessControlMap processControlMap)
        {
            this.pluginTools = pluginTools;
            this.processControlMap = processControlMap;
        }

        public IArchivistInstance[] BringOnline(ArchivistSetup archivistSetup)
        {
            lock (_lock)
            {
                LogSeparator();
                Log($"Starting {archivistSetup.Describe()}...");

                return StartArchivistBinaries(archivistSetup, archivistSetup.NumberOfNodes);
            }
        }

        public void Decommission()
        {
            lock (_lock)
            {
                processControlMap.StopAll();
            }
        }

        private IArchivistInstance[] StartArchivistBinaries(ArchivistStartupConfig startupConfig, int numberOfNodes)
        {
            var result = new List<IArchivistInstance>();
            for (var i = 0; i < numberOfNodes; i++)
            {
                result.Add(StartBinary(startupConfig));
            }

            return result.ToArray();
        }

        private IArchivistInstance StartBinary(ArchivistStartupConfig config)
        {
            var name = GetName(config);
            var dataDir = Path.Combine(dataParentDir, $"datadir_{numberSource.GetNextNumber()}");
            var pconfig = new ArchivistProcessConfig(name, freePortFinder, dataDir);
            Log(pconfig);

            var factory = new ArchivistProcessRecipe(pconfig, archivistExePath);
            var recipe = factory.Initialize(config);

            var startInfo = new ProcessStartInfo(
                fileName: recipe.Cmd,
                arguments: recipe.Args
            );
            //startInfo.UseShellExecute = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            if (process == null || process.HasExited)
            {
                throw new Exception("Failed to start");
            }

            var local = "localhost";
            var instance = new ArchivistInstance(
                name: name,
                imageName: "binary",
                startUtc: DateTime.UtcNow,
                discoveryEndpoint: new Address("Disc", pconfig.LocalIpAddrs.ToString(), pconfig.DiscPort),
                apiEndpoint: new Address("Api", "http://" + local, pconfig.ApiPort),
                listenEndpoint: new Address("Listen", local, pconfig.ListenPort),
                ethAccount: null,
                metricsEndpoint: null
            );

            var pc = new BinaryProcessControl(pluginTools.GetLog(), process, pconfig);
            processControlMap.Add(instance, pc);

            return instance;
        }

        private string GetName(ArchivistStartupConfig config)
        {
            if (!string.IsNullOrEmpty(config.NameOverride))
            {
                return config.NameOverride + "_" + numberSource.GetNextNumber();
            }
            return "archivist_" + numberSource.GetNextNumber();
        }

        private void LogSeparator()
        {
            Log("----------------------------------------------------------------------------");
        }

        private void Log(ArchivistProcessConfig pconfig)
        {
            Log(
                "NodeConfig:Name=" + pconfig.Name +
                "ApiPort=" + pconfig.ApiPort +
                "DiscPort=" + pconfig.DiscPort +
                "ListenPort=" + pconfig.ListenPort +
                "DataDir=" + pconfig.DataDir
            );
        }

        private void Log(string message)
        {
            pluginTools.GetLog().Log(message);
        }

        private static void DeleteParentDataDir()
        {
            if (Directory.Exists(dataParentDir))
            {
                Directory.Delete(dataParentDir, true);
            }
        }

        private static void StopAllArchivistProcesses()
        {
            var processes = Process.GetProcesses();
            var archivistes = processes.Where(p =>
                p.ProcessName.ToLowerInvariant() == "archivist" &&
                p.MainModule != null &&
                p.MainModule.FileName == archivistExePath.Get()
            ).ToArray();

            foreach (var c in archivistes)
            {
                c.Kill();
                c.WaitForExit();
            }
        }
    }
}
