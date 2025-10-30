using Logging;
using Prometheus;

namespace MetricsServer
{
    public class MetricsEvent
    {
        private readonly ILog log;
        private readonly Gauge gauge;
        private readonly TimeSpan eventDuration;
        private readonly object _lock = new object();
        private readonly List<DateTime> utcs = new List<DateTime>();
        private Task? worker = null;

        public MetricsEvent(ILog log, Gauge gauge, TimeSpan eventDuration)
        {
            this.log = log;
            this.gauge = gauge;
            this.eventDuration = eventDuration;
        }

        public void Now()
        {
            lock (_lock)
            {
                utcs.Add(DateTime.UtcNow + eventDuration);
                UpdateGauge();
                if (worker == null) worker = Task.Run(TimeoutEntries);
            }
        }

        private void TimeoutEntries()
        {
            try
            {
                while (true)
                {
                    var sleepTime = GetSleepTime();
                    if (sleepTime == null) return;

                    Thread.Sleep(sleepTime.Value);
                    lock (_lock)
                    {
                        utcs.RemoveAll(entry => DateTime.UtcNow > entry);
                        UpdateGauge();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Exception in {nameof(MetricsEvent)}.{nameof(TimeoutEntries)}: {ex}");
                lock (_lock)
                {
                    worker = null;
                }
            }
        }

        private TimeSpan? GetSleepTime()
        {
            lock (_lock)
            {
                if (utcs.Count == 0)
                {
                    worker = null;
                    return null;
                }
                var now = DateTime.UtcNow;
                foreach (var utc in utcs)
                {
                    if (utc > now)
                    {
                        return utc - now;
                    }
                }

                // all utcs were in the past
                // cleanup everything
                utcs.Clear();
                UpdateGauge();
                worker = null;
                return null;
            }
        }

        private void UpdateGauge()
        {
            gauge.Set(utcs.Count);
        }
    }
}
