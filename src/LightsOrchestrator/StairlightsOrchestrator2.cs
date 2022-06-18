namespace LightsOrchestrator
{
    using Sunset;
    using System.Timers;
    using Microsoft.Extensions.Logging;

    public class StairlightsOrchestrator2 : Timer, ILightsOrchestrator
    {
        private ISunsetTracker sunsetTracker;
        private ILightToggler lightToggler;
        private ILogger<StairlightsOrchestrator2> logger;
        private IConfiguration configs;
        private IDateProvider DateTime;

        private bool lightsOn;
        private ILightTypes stairLightType = new Stair();

        public StairlightsOrchestrator2(ILightToggler lightToggler, ISunsetTracker sunsetTracker, ILogger<StairlightsOrchestrator2> logger, IConfiguration configs, IDateProvider dateProvider)
        {
            this.sunsetTracker = sunsetTracker;
            this.lightToggler = lightToggler;
            this.logger = logger;
            this.configs = configs;
            this.DateTime = dateProvider;            
        }

        public void Setup()
        {
            logger.LogInformation("Setting up next light event.");
            var timeToOn = (sunsetTracker.Today.SunsetLightsOn - this.DateTime.Now).TotalMilliseconds > 0 
                ? sunsetTracker.Today.SunsetLightsOn - this.DateTime.Now
                : sunsetTracker.Tomorrow.SunsetLightsOn - this.DateTime.Now;

            var timeToOff = (sunsetTracker.Today.SunsetLightsOut - this.DateTime.Now).TotalMilliseconds > 0 
                ? sunsetTracker.Today.SunsetLightsOut - this.DateTime.Now
                : sunsetTracker.Tomorrow.SunsetLightsOut - this.DateTime.Now;

            this.lightsOn = timeToOn < timeToOff; 
            TimeSpan nextEvent = lightsOn 
                ? timeToOn 
                : timeToOff; 
            string lightsOnString = lightsOn ? "On" : "Off";
            logger.LogInformation("Lights to be turned {lightsOnString} in {nextEvent.TotalMinutes} minutes", lightsOnString, nextEvent.TotalMinutes);  
            this.Elapsed += LightsElapsed;
            this.Interval = nextEvent.TotalMilliseconds;

            this.Enabled = true;
            this.AutoReset = false;
            this.Start();
        }

        public void LightsElapsed(object sender, ElapsedEventArgs args)
        {
            ToggleLights(this.lightsOn);
        }

        public virtual void ToggleLights(bool on)
        {
            this.Elapsed -= LightsElapsed;
            string lightsOnString = on ? "On" : "Off";
            logger.LogInformation("Turning lights {lightsOnString}", lightsOnString);

            this.lightToggler.ToggleAllLights(stairLightType, on);

            Setup();
        }
    }
}