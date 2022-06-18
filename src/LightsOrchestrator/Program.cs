namespace LightsOrchestrator
{
    using Sunset;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.ApplicationInsights;
    using Microsoft.Extensions.Logging.EventLog;
    using LightsOrchestrator.DAL;

    public class Program
    {
        public static IServiceProvider container;
        public static async Task Main()
        {
            try
            {
                RegisterTypes();
                List<ILightsOrchestrator> orchestratorCollection = new List<ILightsOrchestrator>();
                orchestratorCollection.Add(container.GetService<StairlightsOrchestrator2>());
                orchestratorCollection.Add(container.GetService<MushroomLightsOrchestrator>());
                foreach(ILightsOrchestrator orchestrator in orchestratorCollection)
                {
                    orchestrator.Setup();
                }

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Task.Delay(-1);
            }
        }

        public static void RegisterTypes()
        {
            string appInsightsAppId = new Configuration().ApplicationInsightsId;

            IServiceCollection serviceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration, Configuration>()
                .AddSingleton<ILightStatusChecker, LightStatusChecker>()
                .AddTransient<ILightToggler, LightToggler>()
                .AddSingleton<LightTogglerFactory>()
                .AddSingleton<ISunsetTracker, SunsetCacheTracker>()
                .AddSingleton<IMetrics, AppInsightsMetricProvider>()
                .AddSingleton<StairlightsOrchestrator>()
                .AddSingleton<StairlightsOrchestrator2>()
                .AddSingleton<MushroomLightsOrchestrator>()
                .AddTransient<HttpClientHandler>()
                .AddSingleton<IDateProvider, DateProvider>()
                .AddTransient<Results>()
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddSingleton(typeof(IDataProvider<>), typeof(CacheFileDataProvider<>))
                .AddLogging(logging =>
                {
                    logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
                    logging.AddApplicationInsights(appInsightsAppId);
                    logging.AddConsole();
                    logging.AddEventLog(x => 
                    {
                        x.LogName = nameof(LightsOrchestrator);
                        x.SourceName = nameof(LightsOrchestrator);
                    });
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .AddApplicationInsightsTelemetryWorkerService(appInsightsAppId);                

            container = serviceCollection.BuildServiceProvider();
        }
    }
}