using Logging;

namespace AutoClient
{
    public class LoadBalancer
    {
        private readonly List<Cdx> instances;
        private readonly object instanceLock = new object();

        private class Cdx
        {
            private readonly ILog log;
            private readonly ArchivistWrapper instance;
            private readonly List<Action<ArchivistWrapper>> queue = new List<Action<ArchivistWrapper>>();
            private readonly object queueLock = new object();
            private bool running = true;
            private Task worker = Task.CompletedTask;

            public Cdx(App app, ArchivistWrapper instance)
            {
                Id = instance.Node.GetName();
                log = new LogPrefixer(app.Log, $"[Queue-{Id}]");
                this.instance = instance;
            }

            public string Id { get; }

            public void Start()
            {
                worker = Task.Run(Worker);
            }

            public void Stop()
            {
                running = false;
                worker.Wait();
            }

            public void CheckErrors()
            {
                if (worker.IsFaulted) throw worker.Exception;
            }

            public void Queue(Action<ArchivistWrapper> action)
            {
                if (queue.Count > 2) log.Log("Queue full. Waiting...");
                while (queue.Count > 2)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5.0));
                }

                lock (queueLock)
                {
                    queue.Add(action);
                }
            }

            private void Worker()
            {
                try
                {
                    while (running)
                    {
                        while (queue.Count == 0) Thread.Sleep(TimeSpan.FromSeconds(5.0));

                        Action<ArchivistWrapper> action = w => { };
                        lock (queueLock)
                        {
                            action = queue[0];
                            queue.RemoveAt(0);
                        }

                        action(instance);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Exception in worker: " + ex);
                    throw;
                }
            }
        }

        public LoadBalancer(App app, ArchivistWrapper[] instances)
        {
            this.instances = instances.Select(i => new Cdx(app, i)).ToList();
        }

        public void Start()
        {
            foreach (var i in instances) i.Start();
        }

        public void Stop()
        {
            foreach (var i in instances) i.Stop();
        }

        public void DispatchOnArchivist(Action<ArchivistWrapper> action)
        {
            lock (instanceLock)
            {
                var i = instances.First();
                instances.RemoveAt(0);
                instances.Add(i);

                i.Queue(action);
            }
        }

        public void DispatchOnSpecificArchivist(Action<ArchivistWrapper> action, string id)
        {
            lock (instanceLock)
            {
                var i = instances.Single(a => a.Id == id);
                instances.Remove(i);
                instances.Add(i);

                i.Queue(action);
            }
        }

        public void CheckErrors()
        {
            lock (instanceLock)
            {
                foreach (var i in instances) i.CheckErrors();
            }
        }
    }
}
