using ArchivistClient;
using IdentityModel.Client;
using Logging;
using Utils;
using WebUtils;

namespace BiblioTech.ArchivistChecking
{
    public class ArchivistWrapper
    {
        private readonly ArchivistNodeFactory factory;
        private readonly ILog log;
        private readonly Configuration config;
        private readonly object archivistLock = new object();
        private IArchivistNode? currentArchivistNode;

        public ArchivistWrapper(ILog log, Configuration config)
        {
            this.log = log;
            this.config = config;

            var httpFactory = CreateHttpFactory();
            factory = new ArchivistNodeFactory(log, httpFactory, dataDir: config.DataPath);

            Task.Run(CheckArchivistNode);
        }

        public T? OnArchivist<T>(Func<IArchivistNode, T> func) where T : class
        {
            lock (archivistLock)
            {
                if (currentArchivistNode == null) return null;
                return func(currentArchivistNode);
            }
        }

        private void CheckArchivistNode()
        {
            Thread.Sleep(TimeSpan.FromSeconds(10.0));

            while (true)
            {
                lock (archivistLock)
                {
                    var newNode = GetNewArchivistNode();
                    if (newNode != null && currentArchivistNode == null) ShowConnectionRestored();
                    if (newNode == null && currentArchivistNode != null) ShowConnectionLost();
                    currentArchivistNode = newNode;
                }

                Thread.Sleep(TimeSpan.FromMinutes(15.0));
            }
        }

        private IArchivistNode? GetNewArchivistNode()
        {
            try
            {
                if (currentArchivistNode != null)
                {
                    try
                    {
                        // Current instance is responsive? Keep it.
                        var info = currentArchivistNode.GetDebugInfo();
                        if (info != null && info.Version != null &&
                            !string.IsNullOrEmpty(info.Version.Revision)) return currentArchivistNode;
                    }
                    catch
                    {
                    }
                }

                return CreateArchivist();
            }
            catch (Exception ex)
            {
                log.Error("Exception when trying to check archivist node: " + ex.Message);
                return null;
            }
        }

        private void ShowConnectionLost()
        {
            Program.AdminChecker.SendInAdminChannel("Archivist node connection lost.");
        }

        private void ShowConnectionRestored()
        {
            Program.AdminChecker.SendInAdminChannel("Archivist node connection restored.");
        }

        private IArchivistNode CreateArchivist()
        {
            var endpoint = config.ArchivistEndpoint;
            var splitIndex = endpoint.LastIndexOf(':');
            var host = endpoint.Substring(0, splitIndex);
            var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

            var address = new Address(
                logName: $"cdx@{host}:{port}",
                host: host,
                port: port
            );

            var instance = ArchivistInstance.CreateFromApiEndpoint("ac", address);
            return factory.CreateArchivistNode(instance);
        }

        private HttpFactory CreateHttpFactory()
        {
            if (string.IsNullOrEmpty(config.ArchivistEndpointAuth) || !config.ArchivistEndpointAuth.Contains(":"))
            {
                return new HttpFactory(log, new SnappyTimeSet());
            }

            var tokens = config.ArchivistEndpointAuth.Split(':');
            if (tokens.Length != 2) throw new Exception("Expected '<username>:<password>' in ArchivistEndpointAuth parameter.");

            return new HttpFactory(log, new SnappyTimeSet(), onClientCreated: client =>
            {
                client.SetBasicAuthentication(tokens[0], tokens[1]);
            });
        }

        public class SnappyTimeSet : IWebCallTimeSet
        {
            public TimeSpan HttpCallRetryDelay()
            {
                return TimeSpan.FromSeconds(1.0);
            }

            public TimeSpan HttpCallTimeout()
            {
                return TimeSpan.FromSeconds(3.0);
            }

            public TimeSpan HttpRetryTimeout()
            {
                return TimeSpan.FromSeconds(12.0);
            }
        }
    }
}
