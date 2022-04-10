namespace LightsTester
{
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using LightsOrchestrator;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using LightsOrchestrator.Sunset;
 
    [TestClass]
    public class StairightsOrchestratorTests
    {
        static TestContext testContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            testContext = context;
        }

        [TestMethod]
        public async Task TestLightsOnTime()
        {
            Mock<IDateProvider> dateTimeMock = new Mock<IDateProvider>();
            dateTimeMock.SetupGet(x => x.Now).Returns(() => 
            {
                // Just before 7:30pm when lights should come on
                var target = DateTime.Today.AddHours(19).AddMinutes(30).AddMilliseconds(-500);
                var offset = target - DateTime.Now;
                return DateTime.Now.Add(offset);
            });

            Mock<StairlightsOrchestrator> lo = new Mock<StairlightsOrchestrator>(GetStatusCheckerMock().Object, GetLightTogglerMock().Object, GetSunsetTracker().Object, GetMockLogger().Object, new Configuration(), dateTimeMock.Object) {CallBase = true};
            bool lightsOn = false;
            lo.Setup(x => x.ToggleLightsAsync(It.IsAny<bool>())).Returns(async (bool b) => 
            {
                lightsOn = b;
                return Task.CompletedTask;
            });
            
            await lo.Object.SetupAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.AreEqual(true, lightsOn, "Lights are not on as expected");
        }

        [TestMethod]
        public async Task TestLightsOffTime()
        {
            Mock<IDateProvider> dateTimeMock = new Mock<IDateProvider>();
            dateTimeMock.SetupGet(x => x.Now).Returns(() => 
            {
                // Just before 10:00pm just before lights are turned back off.
                var target = DateTime.Today.AddHours(22).AddMilliseconds(-500);
                var offset = target - DateTime.Now;
                return DateTime.Now.Add(offset);
            });

            Mock<StairlightsOrchestrator> lo = new Mock<StairlightsOrchestrator>(GetStatusCheckerMock().Object, GetLightTogglerMock().Object, GetSunsetTracker().Object, GetMockLogger().Object, new Configuration(), dateTimeMock.Object) {CallBase = true};
            bool lightsOn = true;
            lo.Setup(x => x.ToggleLightsAsync(It.IsAny<bool>())).Returns(async (bool b) => 
            {
                lightsOn = b;
                return Task.CompletedTask;
            });
            
            await lo.Object.SetupAsync();

            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.AreEqual(false, lightsOn, "Lights are not on as expected");
        }

        private Mock<ILogger<StairlightsOrchestrator>> GetMockLogger()
        {
            Mock<ILogger<StairlightsOrchestrator>> mockLogger = new Mock<ILogger<StairlightsOrchestrator>>();
            return mockLogger;
        }

        private Mock<ILightStatusChecker> GetStatusCheckerMock(Dictionary<int, bool> lightStates = null)
        {
            lightStates = lightStates is not null ? lightStates : new Dictionary<int, bool> {{1, false}, {2, false}};
            Mock<ILightStatusChecker> mock = new Mock<ILightStatusChecker>();
            foreach(var lightState in lightStates)
            {
                mock.Setup(x => x.IsLightOnAsync(new Stair(), lightState.Key)).Returns(Task.FromResult(lightState.Value));
            }

            return mock;
        }

        private Mock<ILightToggler> GetLightTogglerMock(Dictionary<int, Exception> lightToggles = null)
        {
            lightToggles = lightToggles ?? new Dictionary<int, Exception> {{1, null}, {2, null}};
            Mock<ILightToggler> mock = new Mock<ILightToggler>();
            foreach(var lightToggle in lightToggles)
            {
                mock.Setup(x => x.ToggleLightsAsync(new Stair(), lightToggle.Key)).Returns(() => 
                {
                    if (lightToggle.Value is not null)
                    {
                        throw lightToggle.Value;
                    }
                    return Task.CompletedTask;
                });
            }

            return mock;
        }

        private Mock<ISunsetTracker> GetSunsetTracker(DailySettings today = null, DailySettings tomorrow = null)
        {
            Results todayResults = new Results(new Configuration());
            todayResults.sunset = DateTime.Today.AddHours(20);
            todayResults.sunrise = DateTime.Today.AddHours(7);
            today = today is not null ? today : new DailySettings(todayResults, new Configuration());

            Results tomorrowResults = new Results(new Configuration());
            tomorrowResults.sunset = DateTime.Today.AddDays(1).AddHours(20);
            tomorrowResults.sunrise = DateTime.Today.AddDays(1).AddHours(7);
            tomorrow = tomorrow is not null ? tomorrow : new DailySettings(tomorrowResults, new Configuration());

            Mock<ISunsetTracker> mock = new Mock<ISunsetTracker>();
            mock.SetupGet(x => x.Today).Returns(today);
            mock.SetupGet(x => x.Tomorrow).Returns(tomorrow);            

            return mock;
        }
    }
}