using DroneStatusFunctionApp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DroneStatusFunction.Tests
{
    public class GetStatusFunctionFixture
    {
        public GetStatusFunctionFixture()
        {
            Environment.SetEnvironmentVariable("CosmosDBEndpoint", "http://testuri.com");
            Environment.SetEnvironmentVariable("CosmosDBKey", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx==");
            Environment.SetEnvironmentVariable("CosmosDBDatabase", "db");
            Environment.SetEnvironmentVariable("CosmosDBCollection", "col");
        }

        [Fact]
        public void Function_ReturnsBadRequest_ForMissingDeviceId()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("roles", GetStatusFunction.GetDeviceStatusRoleName)}));
            var logger = new MockLogger();

            var result = GetStatusFunction.Run(request.Object, null, principal, logger);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Function_ReturnsUnauthorized_ForPrincipalWithoutRoles()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());

            var principal = new ClaimsPrincipal();
            var logger = new MockLogger();

            var result = GetStatusFunction.Run(request.Object, null, principal, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Warning && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "roles" && (string)p.Value == GetStatusFunction.GetDeviceStatusRoleName));
        }

        [Fact]
        public void Function_ReturnsUnauthorized_ForPrincipalWithNonRequiredRoles()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("SomeRole", "SomeOtherRole")}));
            var logger = new MockLogger();

            var result = GetStatusFunction.Run(request.Object, null, principal, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Warning && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "roles" && (string)p.Value == GetStatusFunction.GetDeviceStatusRoleName));
        }

        [Fact]
        public void Function_ReturnsNotFound_ForMissingDocument()
        {
            var queryValues = new Dictionary<string, StringValues>
            {
                { "deviceId", "device1" }
            };

            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection(queryValues));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("roles", GetStatusFunction.GetDeviceStatusRoleName)}));
            var logger = new MockLogger();

            var result = GetStatusFunction.Run(request.Object, null, principal, logger);

            Assert.IsType<NotFoundResult>(result);
        }

        private class MockLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                Entries.Add((logLevel: logLevel, eventId: eventId, state: (object)state, exception: exception, formattedMessage: formatter(state, exception)));
            }

            public IList<(LogLevel logLevel, EventId eventId, object state, Exception exception, string formattedMessage)> Entries { get; }
                = new List<(LogLevel logLevel, EventId eventId, object state, Exception exception, string formattedMessage)>();
        }
    }
}
