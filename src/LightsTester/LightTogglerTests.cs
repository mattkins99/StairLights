namespace LightsTester
{
    using System.Net;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.Protected;
    using LightsOrchestrator;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Threading;
    using System;

    [TestClass]
    public class lightTogglerTests
    {
        static TestContext testContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            testContext = context;
        }

        [TestMethod]
        public async Task TestLightTogglerHappyPath()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.OK, "On");
            LightToggler toggler = new LightToggler(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            toggler.ToggleLights(new Stair(), 1);            
            mockHandler.Verify();
        }

        private Mock<ILogger<LightToggler>> GetMockLogger()
        {
            Mock<ILogger<LightToggler>> mockLogger = new Mock<ILogger<LightToggler>>();
            return mockLogger;
        }

        private Mock<HttpClientHandler> GetMockMessageHandler(HttpStatusCode statusCode, string content)
        {
            var mockHandler = new Mock<HttpClientHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("Send", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            }).Verifiable();

            return mockHandler;
        }
    }
}