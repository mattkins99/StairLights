namespace LightsOrchestrator
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http.Headers;
    using Microsoft.Extensions.Logging;

    public interface ILightToggler
    {
        Task ToggleLightsAsync(int? light = null);
    }

    public class LightToggler : ILightToggler
    {
        IConfiguration configs;

        ILogger<LightToggler> logger;

        HttpClientHandler handler;

        IMetrics metrics;

        public LightToggler(IConfiguration configs, ILogger<LightToggler> logger, HttpClientHandler handler, IMetrics metrics)
        {
            this.configs = configs;
            this.logger = logger;
            this.metrics = metrics;
            this.handler = handler;
            this.handler.ServerCertificateCustomValidationCallback += (a, b, c, d) => true;
        }

        public async Task ToggleLightsAsync(int? light = null)
        {
            var result = await this.CallToggleApiAsync(light);

            var statusCode = result.StatusCode;
            // var responseBody = await result.Content.ReadAsStringAsync();

            if (statusCode == HttpStatusCode.OK)
            {
                metrics.TrackMetric($"{nameof(LightToggler)}_ToggleLightSuccess", 1);
                logger.LogTrace("Call successful");
            }
            else
            {                
                metrics.TrackMetric($"{nameof(LightToggler)}_ToggleLightFalure", 1);
                logger.LogError("ResponseCode: {statusCode}", statusCode);
                throw new ApplicationException("Unable to toggle light.");
            }
        }

        private async Task<HttpResponseMessage> CallToggleApiAsync(int? light = null)
        {
            metrics.TrackMetric($"{nameof(LightToggler)}_ToggleLight", 1);
            var uri = string.Format($"{configs.BaseLightPostUri}{configs.StairsControllerPath}{(light == null ? "" : $"?{configs.StairParam}={light}")}");
            logger.LogTrace("Calling: POST {uri}", uri);
            HttpClient client = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", configs.EncodedCreds);
            HttpResponseMessage result;
            var start = DateTime.Now;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                result = await client.SendAsync(request);
                metrics.TrackDependency(nameof(LightsOrchestrator), nameof(LightToggler), nameof(CallToggleApiAsync), sw.Elapsed, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error calling Light Toggler API. Type: {e.GetType()} Message: {e.Message}", e.GetType(), e.Message);
                metrics.TrackException(e, nameof(LightsOrchestrator), nameof(LightToggler), nameof(CallToggleApiAsync));
                metrics.TrackDependency(nameof(LightsOrchestrator), nameof(LightToggler), nameof(CallToggleApiAsync), sw.Elapsed, false);
                throw new ApplicationException($"Error calling Light Toggler API. Type: {e.GetType()} Message: {e.Message}", e);
            }
            return result;
        }
    }
}