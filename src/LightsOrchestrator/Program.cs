namespace LightsOrchestrator
{
    using Sunset;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.ApplicationInsights;

    public class Program
    {
        public static IServiceProvider container;
        public static async Task Main()
        {
            try
            {
                RegisterTypes();
                ILightsOrchestrator orchestrator = container.GetService<ILightsOrchestrator>();
                await orchestrator.SetupAsync();
                
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
            IServiceCollection serviceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration, Configuration>()
                .AddSingleton<ILightStatusChecker, LightStatusChecker>()
                .AddTransient<ILightToggler, LightToggler>()
                .AddSingleton<LightTogglerFactory>()
                .AddSingleton<ISunsetTracker, SunsetTracker>()
                .AddSingleton<IMetrics, AppInsightsMetricProvider>()
                .AddSingleton<ILightsOrchestrator, StairlightsOrchestrator>()
                .AddTransient<HttpClientHandler>()
                .AddSingleton<IDateProvider, DateProvider>()
                .AddTransient<Results>()
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddLogging(logging =>
                {
                    logging.AddFilter<ApplicationInsightsLoggerProvider>("Category", LogLevel.Trace);
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddApplicationInsights(new Configuration().ApplicationInsightsId);
                })
                .AddApplicationInsightsTelemetryWorkerService(new Configuration().ApplicationInsightsId);                

            container = serviceCollection.BuildServiceProvider();
        }
    }
}