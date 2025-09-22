using Logging;

namespace AutoClient
{
    public class LoadBalancer
    {
        private readonly List<Arch> instances;
        private readonly object instanceLock = new object();
        private readonly App app;

        private class Arch
        {
            private readonly ILog log;
            private readonly ArchivistWrapper instance;
            private readonly List<Action<ArchivistWrapper>> queue = new List<Action<ArchivistWrapper>>();
            private readonly object queueLock = new object();
            private bool running = true;
            private Task worker = Task.CompletedTask;

            public Arch(App app, ArchivistWrapper instance)
            {
                Id = instance.Node.GetName();
                log = new LogPrefixer(app.Log, $"[Queue-{Id}]");
                this.instance = instance;
            }

            public string Id { get; }
            public int QueueSize => queue.Count;

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
                if (queue.Count > 3) Thread.Sleep(TimeSpan.FromSeconds(5.0));
                if (queue.Count > 5) log.Log("Queue full. Waiting...");
                while (queue.Count > 5)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1.0));
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
                        while (running && queue.Count == 0) Thread.Sleep(TimeSpan.FromSeconds(1.0));
                        if (!running) return;

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

        private class ArchComparer : IComparer<Arch>
        {
            public int Compare(Arch? x, Arch? y)
            {
                if (x == null || y == null) return 0;
                return x.QueueSize - y.QueueSize;
            }
        }

        public LoadBalancer(App app, ArchivistWrapper[] instances)
        {
            this.instances = instances.Select(i => new Arch(app, i)).ToList();
            this.app = app;
        }

        public void Start()
        {
            app.Log.Log("LoadBalancer starting...");
            foreach (var i in instances) i.Start();
        }

        public void Stop()
        {
            app.Log.Log("LoadBalancer stopping...");
            foreach (var i in instances) i.Stop();
        }

        public void DispatchOnArchivist(Action<ArchivistWrapper> action)
        {
            lock (instanceLock)
            {
                instances.Sort(new ArchComparer());
                var i = instances.First();

                i.Queue(action);
            }
        }

        public void DispatchOnSpecificArchivist(Action<ArchivistWrapper> action, string id)
        {
            lock (instanceLock)
            {
                var i = instances.Single(a => a.Id == id);
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
