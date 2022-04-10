namespace LightsOrchestrator
{
    using Sunset;
    using System.Timers;
    using Microsoft.Extensions.Logging;

    public class MushroomLightsOrchestrator : Timer, ILightsOrchestrator
    {
        private ILightStatusChecker statusChecker;
        private ISunsetTracker sunsetTracker;
        private ILightToggler lightToggler;
        private ILogger<MushroomLightsOrchestrator> logger;
        private IConfiguration configs;
        private IDateProvider DateTime;
        private ILightTypes mushroomLightType = new Mushroom();

        public MushroomLightsOrchestrator(ILightStatusChecker statusChecker, ILightToggler lightToggler, ISunsetTracker sunsetTracker, ILogger<MushroomLightsOrchestrator> logger, IConfiguration configs, IDateProvider dateProvider)
        {
            this.statusChecker = statusChecker;
            this.sunsetTracker = sunsetTracker;
            this.lightToggler = lightToggler;
            this.logger = logger;
            this.configs = configs;
            this.DateTime = dateProvider;            
        }

        public async Task SetupAsync()
        {
            logger.LogInformation("Setting up next light event.");
            var timeToOn = (sunsetTracker.Today.Sunset - this.DateTime.Now).TotalMilliseconds > 0 
                ? sunsetTracker.Today.Sunset - this.DateTime.Now
                : sunsetTracker.Tomorrow.Sunset - this.DateTime.Now;

            var timeToOff = (sunsetTracker.Today.Sunrise - this.DateTime.Now).TotalMilliseconds > 0 
                ? sunsetTracker.Today.Sunrise - this.DateTime.Now
                : sunsetTracker.Tomorrow.Sunrise - this.DateTime.Now;

            bool lightsOn = timeToOn < timeToOff; 
            TimeSpan nextEvent = lightsOn 
                ? timeToOn 
                : timeToOff; 
            string lightsOnString = lightsOn ? "On" : "Off";
            logger.LogInformation("Lights to be turned {lightsOnString} in {nextEvent.TotalMinutes} minutes", lightsOnString, nextEvent.TotalMinutes);  
            this.Elapsed += async (s, e) => await ElapsedAsync(s, e, lightsOn);
            this.Interval = nextEvent.TotalMilliseconds;

            this.Enabled = true;
            this.AutoReset = false;
            this.Start();
        }

        public async Task ElapsedAsync(object sender, ElapsedEventArgs args, bool on)
        {
            await ToggleLightsAsync(on);
        }

        public virtual async Task ToggleLightsAsync(bool on)
        {
            string lightsOnString = on ? "On" : "Off";
            logger.LogInformation("Turning lights {lightsOnString}", lightsOnString);
            foreach (var light in configs.ControlledLights) 
            {
                if (on != await statusChecker.IsLightOnAsync(mushroomLightType, light))
                {
                    await this.lightToggler.ToggleLightsAsync(mushroomLightType, light);
                }
                else
                {
                    var stateString = on ? "On" : "Off";
                    logger.LogTrace("Light {light} is already {stateString}}.", light, stateString);
                }
            }

            this.Elapsed -= async (s, e) => await ElapsedAsync(s, e, on);
            await SetupAsync();
        }
    }
}