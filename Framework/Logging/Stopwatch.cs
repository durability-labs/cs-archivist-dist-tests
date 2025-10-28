using Utils;

namespace Logging
{
    public class Stopwatch
    {
        private readonly DateTime start = DateTime.UtcNow;
        private readonly ILog log;
        private readonly string name;
        private readonly bool debug;

        private Stopwatch(ILog log, string name, bool debug, int skipFrame)
        {
            this.log = log;
            this.name = name;
            this.debug = debug;

            if (debug)
            {
                var entry = $"{name}...";
                log.Debug(entry, skipFrame + 1);
            }
        }

        public static TimeSpan Measure(ILog log, string name, Action action, bool debug = false)
        {
            var sw = Begin(log, name, debug, skipFrame: 1);
            action();
            return sw.End();
        }

        public static StopwatchResult<T> Measure<T>(ILog log, string name, Func<T> action, bool debug = false)
        {
            var sw = Begin(log, name, debug, skipFrame: 1);
            var result = action();
            var duration = sw.End();
            return new StopwatchResult<T>(result, duration);
        }

        public static Stopwatch Begin(ILog log, int skipFrame = 0)
        {
            return Begin(log, "", skipFrame + 1);
        }

        public static Stopwatch Begin(ILog log, string name, int skipFrame = 0)
        {
            return Begin(log, name, false, skipFrame + 1);
        }

        public static Stopwatch Begin(ILog log, bool debug, int skipFrame = 0)
        {
            return Begin(log, "", debug, skipFrame + 1);
        }

        public static Stopwatch Begin(ILog log, string name, bool debug, int skipFrame = 0)
        {
            return new Stopwatch(log, name, debug, skipFrame + 1);
        }

        public TimeSpan End(string msg = "", int skipFrames = 0)
        {
            var duration = DateTime.UtcNow - start;
            var entry = $"{name} {msg} ({Time.FormatDuration(duration)})";

            if (debug)
            {
                log.Debug(entry, skipFrames + 1);
            }
            else
            {
                log.Log(entry);
            }

            return duration;
        }
    }

    public class StopwatchResult<T>
    {
        public StopwatchResult(T value, TimeSpan duration)
        {
            Value = value;
            Duration = duration;
        }

        public T Value { get; }
        public TimeSpan Duration { get; }
    }
}
