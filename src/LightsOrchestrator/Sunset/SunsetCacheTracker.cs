namespace LightsOrchestrator.Sunset
{
    using System.Diagnostics;
    using System.Net;
    using LightsOrchestrator.DAL;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System.Timers;

    public class SunsetCacheTracker : ISunsetTracker
    {
        private static object fetchLock = new object();
        ILogger<SunsetTracker> logger;
        IConfiguration configs;
        IMetrics metrics;
        IDateProvider DateTime;

        public DailySettings todayCache;

        public DailySettings tomorrowCache;
        HttpClientHandler handler;

        Dictionary<DateTime, DailySettings> settingsCache = new Dictionary<DateTime, DailySettings>();

        IDataProvider<DailySettings> cacheProvider;

        Timer timer;

        public DailySettings Today 
        {
            get
            {
                return Task.Run(() => cacheProvider.GetDataByIdAsync(this.DateTime.Now.ToString("yyyy-MM-dd"))).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public DailySettings Tomorrow 
        {
            get
            {
                return Task.Run(() => cacheProvider.GetDataByIdAsync(this.DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"))).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public SunsetCacheTracker(ILogger<SunsetTracker> logger, IDateProvider dateProvider, IConfiguration configs, IMetrics metrics, IDataProvider<DailySettings> cacheProvider)
        {
            this.metrics = metrics;
            this.logger = logger;
            this.configs = configs;
            this.DateTime = dateProvider;
            this.cacheProvider = cacheProvider;

            initailzeCache();

            this.timer = new Timer();
            timer.Elapsed += TimerElapsedAsync;
            timer.AutoReset = true;
            timer.Interval = TimeSpan.FromHours(12).TotalMilliseconds;
            timer.Start(); 
        }

        private void initailzeCache()
        {
            Task.Run(() => FillCache()).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async void TimerElapsedAsync(object sender, ElapsedEventArgs args)
        {
            await FillCache();
        }

        private async Task FillCache()
        {
            for (int i = 0; i < 30; i++)
            {
                string key = this.DateTime.Now.AddDays(i).ToString("yyyy-MM-dd");
                if (await cacheProvider.GetDataByIdAsync(key) == null)
                {
                    var dailySettings = await RefreshDailySettingsAsync(this.DateTime.Now.AddDays(i));
                    await cacheProvider.UpcertAsync(dailySettings);
                }
            }
        }

        private async Task<DailySettings> RefreshDailySettingsAsync(DateTime date)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var result = await this.CallSunsetAPIAsync(date, sw);

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
                        metrics.TrackException(e, nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(RefreshDailySettingsAsync));
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
            catch (ApplicationException)
            {
                // already logged.  just swallow here
            }
            catch (Exception e)
            {
                // Not sure how we could get here, but if we can it's a bug.
                logger.LogError(e, "Unexpected error making call to sunset data API");
                metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
            }        
            
            metrics.TrackDependency(nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(RefreshDailySettingsAsync), sw.Elapsed, false);
            throw new ApplicationException("Unable to get Sunset data");
        }

        private async Task<HttpResponseMessage> CallSunsetAPIAsync(DateTime date, Stopwatch sw)
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
                        metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetSSLError", 1);

                        this.handler = new HttpClientHandler();
                        this.handler.ServerCertificateCustomValidationCallback += (a, b, c, d) => true;
                        continue;
                    }
                    
                    logger.LogError(hre, "Http Error making call to sunset data API");
                    metrics.TrackMetric($"{nameof(SunsetTracker)}_GetSunsetCallFailure", 1);
                    metrics.TrackDependency(nameof(LightsOrchestrator), nameof(SunsetTracker), nameof(CallSunsetAPIAsync), sw.Elapsed, false);                    
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