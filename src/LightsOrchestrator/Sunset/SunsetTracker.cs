namespace LightsOrchestrator.Sunset
{
    using System.Diagnostics;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public interface ISunsetTracker
    {
        DailySettings Today { get; }
        DailySettings Tomorrow { get; }
    }

    public class SunsetTracker : ISunsetTracker
    {
        private static object fetchLock = new object();
        ILogger<SunsetTracker> logger;
        IConfiguration configs;
        IMetrics metrics;
        IDateProvider DateTime;

        public DailySettings todayCache;

        public DailySettings tomorrowCache;
        HttpClientHandler handler;

        public DailySettings Today 
        {
            get
            {
                return Task.Run(() => GetSunsetAsync(this.DateTime.Now)).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public DailySettings Tomorrow 
        {
            get
            {
                return tomorrowCache;
            }
        }

        public SunsetTracker(ILogger<SunsetTracker> logger, IDateProvider dateProvider, IConfiguration configs, IMetrics metrics)
        {
            this.metrics = metrics;
            this.logger = logger;
            this.configs = configs;
            this.DateTime = dateProvider;
        }

        private async Task<DailySettings> GetSunsetAsync(DateTime date)
        {
            lock(fetchLock)
            {
                if (todayCache is null || System.DateTime.Parse(date.ToString("yyyy-MM-dd")) != todayCache.Date)
                {
                    todayCache = tomorrowCache ?? Task.Run(() => RefreshDailySettingsAsync(date)).ConfigureAwait(false).GetAwaiter().GetResult();;
                    tomorrowCache = Task.Run(() => RefreshDailySettingsAsync(date.AddDays(1))).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }

            return todayCache;            
        }

        private async Task<DailySettings> RefreshDailySettingsAsync(DateTime date)
        {
            try
            {
                var result = await this.CallSunsetAPIAsync(date);

                var statusCode = result.StatusCode;
                var responseBody = await result.Content?.ReadAsStringAsync();

                if (statusCode == HttpStatusCode.OK)
                {
                    logger.LogTrace("Call successful: {responseBody}", responseBody);
                    Root response;                
                    try
                    {
                        response = JsonConvert.DeserializeObject<Root>(responseBody);
                        if (response is not null)
                        {
                            metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetSuccess", 1);
                            return new DailySettings(response.results, configs);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error deserializing sunset response.  Body: {responseBody}", responseBody);
                        metrics.TrackException(e, nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(GetSunsetAsync));
                        throw new ApplicationException("Sunset deserialization error", e);
                    }
                    
                    logger.LogError("Sunset API response value is null");
                    metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
                }
                else
                {
                    logger.LogError("ResponseCode: {statusCode} Body: {responseBody}", statusCode, responseBody);
                    metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error making call to sunset data API");
                metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
            }        
            
            throw new ApplicationException("Unable to get Sunset data");
        }


        private async Task<HttpResponseMessage> CallSunsetAPIAsync(DateTime date)
        {
            this.handler = null;
            while (true)
            {
                metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunset", 1);
                var uri = string.Format($"{configs.SunsetGetUri}{date.ToString("yyyy-MM-dd")}");
                logger.LogTrace("Calling: GET {uri}", uri);
                HttpClient client = handler is null 
                    ? new HttpClient()
                    : new HttpClient(handler);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                HttpResponseMessage result;
                var start = DateTime.Now;
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    result = await client.SendAsync(request);
                    metrics.TrackDependency(nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(CallSunsetAPIAsync), sw.Elapsed, true);
                }
                catch (HttpRequestException hre)
                {
                    if (hre.Message == "The SSL connection could not be established, see inner exception.")
                    {
                        logger.LogWarning(hre, "SSL error making call to sunset data API");
                        metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);

                        this.handler = new HttpClientHandler();
                        this.handler.ServerCertificateCustomValidationCallback += (a, b, c, d) => true;
                        continue;
                    }
                    
                    logger.LogError(hre, "Http Error making call to sunset data API");
                    metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);                    
                    throw new ApplicationException($"Error calling Sunset API. Type: {hre.GetType()} Message: {hre.Message}", hre);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error calling Sunset API. Type: {e.GetType()} Message: {e.Message}", e.GetType(), e.Message);
                    metrics.TrackException(e, nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(CallSunsetAPIAsync));
                    metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
                    metrics.TrackDependency(nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(CallSunsetAPIAsync), sw.Elapsed, false);
                    throw new ApplicationException($"Error calling Sunset API. Type: {e.GetType()} Message: {e.Message}", e);
                }
                return result;
            }
        }
    }
}