namespace LightsOrchestrator.Sunset
{
    public class Results
    {
        IConfiguration configs;

        public Results(IConfiguration configs)
        {
            // there has to be a way to deserialize with Newtonsoft and DI.
            this.configs = configs;//Program.container.GetService<IConfiguration>();
        }

        public DateTime sunrise { get; set; }
        public DateTime sunset { get; set; }
        public DateTime solar_noon { get; set; }
        public int day_length { get; set; }
        public DateTime civil_twilight_begin { get; set; }
        public DateTime civil_twilight_end { get; set; }
        public DateTime nautical_twilight_begin { get; set; }
        public DateTime nautical_twilight_end { get; set; }
        public DateTime astronomical_twilight_begin { get; set; }
        public DateTime astronomical_twilight_end { get; set; }

        public DateTime SunsetLightsOut
        {
            get { return sunset.Add(configs.AfterSunsetEnd); }
        }

        public DateTime SunsetLightsOn
        {
            get { return sunset.Add(configs.BeforeSunsetStart); }
        }
    }
}