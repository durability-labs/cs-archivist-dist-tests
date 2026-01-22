using AutoClient.Modes.FolderStore;

namespace AutoClient.Modes
{
    public class FolderStoreMode
    {
        private readonly App app;
        private readonly NodeDispatcher nodeDispatcher;
        private Task checkTask = Task.CompletedTask;

        public FolderStoreMode(App app, NodeDispatcher nodeDispatcher)
        {
            this.app = app;
            this.nodeDispatcher = nodeDispatcher;
        }

        public void Start()
        {
            checkTask = Task.Run(() =>
            {
                try
                {
                    todo: this won't work: we need recoverability from the stored data
                    var folderStatus = new FolderStatus(app);
                    var nodeOperator = new NodeOperator(app.Log, nodeDispatcher);
                    var fileProcessor = new FileProcessor(app, folderStatus, nodeOperator);
                    var folderIterator = new FolderIterator(app, fileProcessor);

                    while (!app.Cts.IsCancellationRequested)
                    {
                        folderIterator.Run();
                        Thread.Sleep(TimeSpan.FromHours(1.0));
                    }
                }
                catch (Exception ex)
                {
                    app.Log.Error("Exception in FolderStoreMode: " + ex);
                    Environment.Exit(1);
                }
            });
        }

        public void Stop()
        {
            app.Cts.Cancel();
            checkTask.Wait();
        }
    }
}
