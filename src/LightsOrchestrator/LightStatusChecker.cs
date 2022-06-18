namespace LightsOrchestrator
{
    using System.Diagnostics;
    using System.Net;
    using Microsoft.Extensions.Logging;

    public interface ILightStatusChecker
    {
        Task<bool> IsLightOnAsync(ILightTypes lightType, int light);
    }

    public class LightStatusChecker : ILightStatusChecker
    {
        IConfiguration configs;
        ILogger<LightStatusChecker> logger;
        HttpClientHandler handler;
        IMetrics metrics;
        private HttpClient httpClient = null;

        public Dictionary<int, bool> LightStatusStates;

        public LightStatusChecker(IConfiguration configs, ILogger<LightStatusChecker> logger, HttpClientHandler handler, IMetrics metrics)
        {
            this.configs = configs;
            this.logger = logger;
            this.handler = handler;
            this.metrics = metrics;
            LightStatusStates = new Dictionary<int, bool>();
        }

        public async Task<bool> IsLightOnAsync(ILightTypes lightType, int light)
        {
            if (!LightStatusStates.ContainsKey(light))
            {
                // default to off
                LightStatusStates[light] = false;
            }

            var result = await this.CallLightStatusAsync(lightType, light);

            var statusCode = result.StatusCode;
            var responseBody = await result.Content?.ReadAsStringAsync();

            if (statusCode == HttpStatusCode.OK)
            {
                logger.LogTrace("Call successful: {responseBody}", responseBody);

                if (responseBody == "On" || responseBody == "Off")
                {
                    bool status = responseBody == "On";
                    LightStatusStates[light] = status;
                }   
                else
                { 
                    logger.LogError("Light Status API response value is invalid");
                    metrics.TrackMetric($"{nameof(LightStatusChecker)}_LightStatusBadData", 1);
                }
            }
            else
            {
                logger.LogError("ResponseCode: {statusCode} Body: {responseBody}", statusCode, responseBody);
                metrics.TrackMetric($"{nameof(LightStatusChecker)}_GetLightStatusResponseCodeFailure", 1);
            }            
            
            // return current value or last known value
            return LightStatusStates[light];
        }

        private async Task<HttpResponseMessage> CallLightStatusAsync(ILightTypes lightType, int light)
        {
            var uri = string.Format($"{configs.BaseLightGetUri}{lightType.Controller}?{lightType.Entity}={light}");
            logger.LogTrace("Calling: GET {uri}", uri);
            httpClient = httpClient ?? new HttpClient(this.handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage result;
            var start = DateTime.Now;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                result = await httpClient.SendAsync(request);
                metrics.TrackDependency(nameof(LightsOrchestrator), nameof(LightStatusChecker), nameof(CallLightStatusAsync), sw.Elapsed, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error calling Light Controller API. Type: {e.GetType()} Message: {e.Message}", e.GetType(), e.Message);
                metrics.TrackException(e, nameof(LightsOrchestrator), nameof(LightStatusChecker), nameof(CallLightStatusAsync));
                metrics.TrackDependency(nameof(LightsOrchestrator), nameof(LightStatusChecker), nameof(CallLightStatusAsync), sw.Elapsed, false);
                httpClient.Dispose();
                httpClient = null;
                throw new ApplicationException($"Error calling Light Controller. Type: {e.GetType()} Message: {e.Message}", e);
            }
            return result;

        }
    }
}