using Logging;
using WebUtils;

namespace AutoClient
{
    public class App
    {
        public App(Configuration config)
        {
            Config = config;

            Log = new TimestampPrefixer(
                new LogSplitter(
                    new FileLog(Path.Combine(config.LogPath, "autoclient")),
                    new ConsoleLog()
                )
            );

            Metrics = new AppMetrics(Log, config);
        }

        public Configuration Config { get; }
        public ILog Log { get; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
        public AppMetrics Metrics { get; }
    }

    public class AutoClientWebTimeSet : IWebCallTimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(30.0);
        }

        public TimeSpan HttpRetryTimeout()
        {
            return HttpCallTimeout() * 2.2;
        }

        /// <summary>
        /// After a failed HTTP call, wait this long before trying again.
        /// </summary>
        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromMinutes(1.0);
        }
    }
}
