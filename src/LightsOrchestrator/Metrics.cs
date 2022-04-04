namespace LightsOrchestrator
{
    using Microsoft.ApplicationInsights;

    public interface IMetrics
    {
        void TrackMetric(string metricName, double value);

        void TrackDependency(string service, string className, string operation, TimeSpan duration, bool isSuccess);

        void TrackException(Exception e, string service, string className, string operation);
    }

    public class AppInsightsMetricProvider :  IMetrics
    {
        TelemetryClient metricClient;

        public AppInsightsMetricProvider(TelemetryClient metricClient)
        {
            this.metricClient = metricClient;
        }
        public void TrackMetric(string metricName, double value)
        {
            metricClient.TrackMetric(metricName, value);
        }

        public void TrackDependency(string service, string className, string operation, TimeSpan duration, bool isSuccess)
        {
            metricClient.TrackDependency(service, className, operation, DateTime.Now - duration, duration, isSuccess);
        }

        public void TrackException(Exception e, string service, string className, string operation)
        {
            metricClient.TrackException(e, new Dictionary<string, string> 
                                                        { 
                                                            { nameof(service), service }, 
                                                            { nameof(className), className }, 
                                                            { nameof(operation), operation } 
                                                        });
        }
        
    }
}