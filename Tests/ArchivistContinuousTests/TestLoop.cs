using DistTestCore.Logs;
using Logging;
using TaskFactory = Utils.TaskFactory;

namespace ContinuousTests
{
    public class TestLoop
    {
        private readonly EntryPointFactory entryPointFactory;
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly ILog overviewLog;
        private readonly StatusLog statusLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;
        private readonly StartupChecker startupChecker;
        private readonly CancellationToken cancelToken;
        private readonly EventWaitHandle runFinishedHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        private static object testLock = new object();

        public TestLoop(EntryPointFactory entryPointFactory, TaskFactory taskFactory, Configuration config, ILog overviewLog, StatusLog statusLog, Type testType, TimeSpan runsEvery, StartupChecker startupChecker, CancellationToken cancelToken)
        {
            this.entryPointFactory = entryPointFactory;
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.statusLog = statusLog;
            this.testType = testType;
            this.runsEvery = runsEvery;
            this.startupChecker = startupChecker;
            this.cancelToken = cancelToken;
            Name = testType.Name;
        }

        public string Name { get; }
        public int NumberOfPasses { get; private set; }
        public int NumberOfFailures { get; private set; }

        public void Begin()
        {
            taskFactory.Run(() =>
            {
                try
                {
                    NumberOfPasses = 0;
                    NumberOfFailures = 0;
                    while (!cancelToken.IsCancellationRequested)
                    {
                        lock (testLock)
                        // In the original design, multiple tests are allowed to interleave their test-moments, increasing test through-put.
                        // Since we're still stabilizing some of the basics, this lock limits us to 1 test run at a time.
                        {
                            WaitHandle.WaitAny(new[] { runFinishedHandle, cancelToken.WaitHandle });

                            cancelToken.ThrowIfCancellationRequested();

                            StartTest();

                            cancelToken.WaitHandle.WaitOne(runsEvery);
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (OperationCanceledException)
                {
                    overviewLog.Log("Test-loop " + testType.Name + " is cancelled.");
                }
                catch (Exception ex)
                {
                    overviewLog.Error("Test infra failure: TestLoop failed with " + ex);
                    Environment.Exit(-1);
                }
            }, nameof(TestLoop));
        }

        private void StartTest()
        {
            var test = (ContinuousTest)Activator.CreateInstance(testType)!;
            var handle = new TestHandle(test);
            var run = new SingleTestRun(entryPointFactory, taskFactory, config, overviewLog, statusLog, handle,
                startupChecker, cancelToken, config.ArchivistDeployment.Id);

            runFinishedHandle.Reset();
            run.Run(runFinishedHandle, result =>
            {
                if (result) NumberOfPasses++;
                else NumberOfFailures++;
            });
        }
    }
}
