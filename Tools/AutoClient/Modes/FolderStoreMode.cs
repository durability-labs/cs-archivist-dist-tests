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
                while (!app.Cts.IsCancellationRequested)
                {
                    RunLoop();
                    Thread.Sleep(TimeSpan.FromHours(1.0));
                }
            });
        }

        private void RunLoop()
        {
            try
            {
                var folderStatus = new FolderStatus(app);
                var nodeOperator = new NodeOperator(app.Log, folderStatus, nodeDispatcher, app.AppEvents);
                var fileProcessor = new FileProcessor(app, folderStatus, nodeOperator, app.AppEvents);
                var purchaseRenewer = new PurchaseRenewer(app, folderStatus, nodeOperator);
                var folderIterator = new FolderIterator(app, fileProcessor);

                folderIterator.Initialize();

                while (!folderIterator.IsFinished && !app.Cts.IsCancellationRequested)
                {
                    folderIterator.Step();
                    purchaseRenewer.Step();

                    app.FailureDelay.ApplyDelay();
                }
                Log("Loop finished.");
            }
            catch (Exception ex)
            {
                app.Log.Error("Exception in FolderStoreMode: " + ex);
                Environment.Exit(1);
            }
        }

        public void Stop()
        {
            app.Cts.Cancel();
            checkTask.Wait();
        }

        private void Log(string v)
        {
            app.Log.Log(v);
        }
    }
}
