namespace LightsTester
{
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Moq.Protected;
    using LightsOrchestrator;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Threading;
    using System;

    [TestClass]
    public class LightStatusCheckerTests
    {
        static TestContext testContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            testContext = context;
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerOn()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.OK, "On");
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            bool response = await statusChecker.IsLightOnAsync(1);

            Assert.AreEqual(true, response, "Light not on as expected");
            mockHandler.Verify();
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerOff()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.OK, "Off");
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            bool response = await statusChecker.IsLightOnAsync(1);

            Assert.AreEqual(false, response, "Light not off as expected");
            mockHandler.Verify();
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerInvalid()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.OK, "invalid");
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            bool response = await statusChecker.IsLightOnAsync(1);

            Assert.AreEqual(false, response, "Light not off as expected");
            mockHandler.Verify();
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerErrorStatusOn()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.NotFound, "On");
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            statusChecker.LightStatusStates[1] = true;

            bool response = await statusChecker.IsLightOnAsync(1);

            Assert.AreEqual(true, response, "Light was not on as expected.");
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerErrorStatusOff()
        {
            var mockHandler = GetMockMessageHandler(HttpStatusCode.NotFound, "On");
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);
            statusChecker.LightStatusStates[1] = false;

            bool response = await statusChecker.IsLightOnAsync(1);

            Assert.AreEqual(false, response, "Light was not off as expected.");
        }

        [TestMethod]
        public async Task TestLightsStatusCheckerThrownExecption()
        {
            var mockHandler = new Mock<HttpClientHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => { throw new SocketException(1); });
            LightStatusChecker statusChecker = new LightStatusChecker(new Configuration(), GetMockLogger().Object, mockHandler.Object, new Mock<IMetrics>().Object);

            await Assert.ThrowsExceptionAsync<ApplicationException>(() => statusChecker.IsLightOnAsync(1), "Expected execption not thrown");
        }

        private Mock<ILogger<LightStatusChecker>> GetMockLogger()
        {
            Mock<ILogger<LightStatusChecker>> mockLogger = new Mock<ILogger<LightStatusChecker>>();
            return mockLogger;
        }

        private Mock<HttpClientHandler> GetMockMessageHandler(HttpStatusCode statusCode, string content)
        {
            var mockHandler = new Mock<HttpClientHandler>();
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            }).Verifiable();

            return mockHandler;
        }
    }
}