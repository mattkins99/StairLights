namespace LightsOrchestrator.Sunset
{
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Newtonsoft.Json.Linq;

    public class DailySettings
    {
        IConfiguration configs;

        public DailySettings(Results results, IConfiguration configs)
        {
            Sunset = results.sunset;
            this.configs = configs;
        }

        public DateTime Sunset { get; }

        private DateTime? sunsetLightsOut;// = DateTime.Now.AddSeconds(20);

        public DateTime SunsetLightsOut
        {
            get { return (sunsetLightsOut ?? Sunset.Add(configs.AfterSunsetEnd)); }
        }

        public DateTime SunsetLightsOn
        {
            get { return Sunset.Add(configs.BeforeSunsetStart); }
        }
    }
}