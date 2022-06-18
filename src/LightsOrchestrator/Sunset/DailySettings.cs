namespace LightsOrchestrator.Sunset
{
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Newtonsoft.Json.Linq;
    using LightsOrchestrator.DAL;

    public class DailySettings : IUnique
    {
        IConfiguration configs;

        public DailySettings()
        {
            configs = Program.container.GetService<IConfiguration>();
        }

        public DailySettings(Results results, IConfiguration configs)
        {
            Sunset = results.sunset;
            Sunrise = results.sunrise;
            Date = DateTime.Parse(results.sunrise.ToString("yyyy-MM-dd"));
            this.configs = configs;
        }

        [JsonIgnore]
        public string Id { get { return Date.ToString("yyyy-MM-dd"); } }

        public DateTime Sunset { get; init; }

        public DateTime Sunrise { get; init; }

        public DateTime Date { get; init; }
        private DateTime? sunsetLightsOut;// = DateTime.Now.AddSeconds(20);

        [JsonIgnore]
        public DateTime SunsetLightsOut
        {
            get { return (sunsetLightsOut ?? Sunset.Add(configs.AfterSunsetEnd)); }
        }

        [JsonIgnore]
        public DateTime SunsetLightsOn
        {
            get { return Sunset.Add(configs.BeforeSunsetStart); }
        }

        [JsonIgnore]
        public int Partition
        {
            get { return (int)(this.Date - DateTime.Today).TotalDays; }
        }

        [JsonIgnore]
        public int MinPartition 
        { 
            get { return 0; } 
        }
    }
}