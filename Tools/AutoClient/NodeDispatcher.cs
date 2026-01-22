using Logging;

namespace AutoClient
{
    public class NodeDispatcher
    {
        private readonly ILog log;
        private readonly List<ArchivistWrapper> nodes;
        private readonly object nodesLock = new object();

        public NodeDispatcher(ILog log, ArchivistWrapper[] nodes)
        {
            this.log = log;
            this.nodes = nodes.ToList();
        }

        public void OnNode(Action<ArchivistWrapper> action, Action whenDone)
        {
            var node = TakeNode();

            Task.Run(() =>
            {
                try
                {
                    action(node);
                    whenDone();
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                }

                ReleaseNode(node);
            });
        }

        private ArchivistWrapper TakeNode()
        {
            var wait = false;
            while (true)
            {
                while (nodes.Count == 0)
                {
                    if (!wait)
                    {
                        wait = true;
                        log.Log("Waiting for Archivist node to become available...");
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                lock (nodesLock)
                {
                    if (nodes.Count > 0)
                    {
                        var node = nodes.First();
                        nodes.RemoveAt(0);
                        return node;
                    }
                }
            }
        }

        private void ReleaseNode(ArchivistWrapper node)
        {
            lock (nodesLock)
            {
                nodes.Add(node);
            }
        }
    }
}
