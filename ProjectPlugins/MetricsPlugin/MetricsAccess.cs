using Core;
using KubernetesWorkflow.Types;
using Utils;

namespace MetricsPlugin
{
    public interface IMetricsAccess : IHasContainer
    {
        string TargetName { get; }
        Metrics GetAllMetrics();
        MetricsSet GetMetric(string metricName);
        MetricsSet GetMetric(string metricName, TimeSpan timeout);
    }

    public class MetricsAccess : IMetricsAccess
    {
        private readonly MetricsQuery query;
        private readonly Address target;

        public MetricsAccess(MetricsQuery query, Address target)
        {
            this.query = query;
            this.target = target;
            TargetName = $"'{target.Host}'";
        }

        public string TargetName { get; }
        public RunningContainer Container => query.RunningContainer;

        public Metrics GetAllMetrics()
        {
            return query.GetAllMetricsForNode(target);
        }

        public MetricsSet GetMetric(string metricName)
        {
            return GetMetric(metricName, TimeSpan.FromSeconds(10));
        }

        public MetricsSet GetMetric(string metricName, TimeSpan timeout)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                var mostRecent = GetMostRecent(metricName);
                if (mostRecent != null) return mostRecent;
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException();
                }

                Time.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        private MetricsSet GetMostRecent(string metricName)
        {
            var result = query.GetMostRecent(metricName, target);
            if (result.Sets.Length == 0)
            {
                throw new Exception($"Metric '{metricName}' was not returned for '{TargetName}'.");
            }

            var selected = result.Sets[0];
            var selectedValue = GetMostRecentValue(selected);

            for (var i = 1; i < result.Sets.Length; i++)
            {
                var candidate = result.Sets[i];
                var candidateValue = GetMostRecentValue(candidate);
                if (candidateValue > selectedValue)
                {
                    selected = candidate;
                    selectedValue = candidateValue;
                }
            }

            return selected;
        }

        private double GetMostRecentValue(MetricsSet set)
        {
            if (set.Values.Length == 0) return double.MinValue;

            var mostRecent = set.Values[0].Value;
            for (var i = 1; i < set.Values.Length; i++)
            {
                if (set.Values[i].Value > mostRecent)
                {
                    mostRecent = set.Values[i].Value;
                }
            }

            return mostRecent;
        }
    }
}
