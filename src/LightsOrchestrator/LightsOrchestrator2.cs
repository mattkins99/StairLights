namespace LightsOrchestrator
{
    using Sunset;
    using System.Timers;
    using Microsoft.Extensions.Logging;

    public class LightsOrchestrator2 : Timer, ILightsOrchestrator
    {
        private ILightStatusChecker statusChecker;
        private ISunsetTracker sunsetTracker;
        private ILightToggler lightToggler;
        private ILogger<LightsOrchestrator> logger;
        private IConfiguration configs;
        private DailySettings dailySettings { get; set; }
        private bool setForTheDay = false;

        public LightsOrchestrator2(ILightStatusChecker statusChecker, ILightToggler lightToggler, ISunsetTracker sunsetTracker, ILogger<LightsOrchestrator> logger, IConfiguration configs)
        {
            this.statusChecker = statusChecker;
            this.sunsetTracker = sunsetTracker;
            this.lightToggler = lightToggler;
            this.logger = logger;
            this.configs = configs;

            this.Interval = configs.Delay.TotalMilliseconds;
            this.Elapsed += async (s, e) => await ElapsedAsync(s, e);
            this.Enabled = true;
        }

        public async Task ElapsedAsync(object sender, ElapsedEventArgs args)
        {
            if (dailySettings is null || DateTime.Parse(dailySettings.Sunset.ToString("yyyy-MM-dd")) < DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd")))
            {
                logger.LogTrace("Getting data");
                dailySettings = await sunsetTracker.GetSunsetAsync(DateTime.Now);
                setForTheDay = false;
            }
            else
            {
                logger.LogTrace("Data is current");
            }

            if (!setForTheDay && DateTime.Now > dailySettings.SunsetLightsOn && DateTime.Now < dailySettings.SunsetLightsOut)
            {
                logger.LogTrace("Time to turn on the lights");
                setForTheDay = true;

                foreach (var light in configs.ControlledLights) 
                {
                    if (!await statusChecker.IsLightOnAsync(light))
                    {
                        await this.lightToggler.ToggleLightsAsync(light);
                    }
                    else
                    {
                        logger.LogTrace("Light {light} is already on.", light);
                    }
                }

                while (DateTime.Now < dailySettings.SunsetLightsOut)
                {
                    var timeRemaining = DateTime.Now - dailySettings.SunsetLightsOut;                        
                    logger.LogTrace("Light time.  Time left {timeRemaining}", timeRemaining);
                    await Task.Delay(configs.Delay);
                }

                logger.LogTrace("Turning lights off");

                foreach (var light in configs.ControlledLights) 
                {
                    if (await statusChecker.IsLightOnAsync(light))
                    {
                        await this.lightToggler.ToggleLightsAsync(light);
                    }
                    else
                    {
                        logger.LogTrace("Light {light} is already off.", light);
                    }
                }
            }
            else if (setForTheDay && DateTime.Now < dailySettings.SunsetLightsOut)
            {
                // Do nothing.  Main thread is still above in the wait block.                
            }
            else
            {
                logger.LogTrace("Outside light times. {DateTime.Now} - {dailySettings.SunsetLightsOn} - {dailySettings.SunsetLightsOut}", DateTime.Now, dailySettings.SunsetLightsOn, dailySettings.SunsetLightsOut);
            }
        }
    }
}