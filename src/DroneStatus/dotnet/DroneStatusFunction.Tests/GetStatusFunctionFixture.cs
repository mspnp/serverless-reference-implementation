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
        public async Task Function_ReturnsUnauthorized_ForMissingPrincipalHeader()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary());

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Error && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "header" && (string)p.Value == HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey));
        }

        [Fact]
        public async Task Function_ReturnsUnauthorized_ForInvalidPrincipalHeader()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary
                {
                    [HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey] = "invalid header value"
                });

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Error && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "header" && (string)p.Value == HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey));
        }

        [Fact]
        public async Task Function_ReturnsBadRequest_ForMissingDeviceId()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary
                {
                    [HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey] = CreatePrincipalWithRoles(GetStatusFunction.GetDeviceStatusRoleName)
                });

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Function_ReturnsUnauthorized_ForPrincipalWithoutRoles()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary
                {
                    [HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey] = CreatePrincipalWithRoles()
                });

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Warning && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "roles" && (string)p.Value == GetStatusFunction.GetDeviceStatusRoleName));
        }

        [Fact]
        public async Task Function_ReturnsUnauthorized_ForPrincipalWithNonRequiredRoles()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary
                {
                    [HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey] = CreatePrincipalWithRoles("SomeRole", "SomeOtherRole")
                });

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<UnauthorizedResult>(result);
            Assert.Contains(
                logger.Entries,
                e => e.logLevel == LogLevel.Warning && e.state is IEnumerable<KeyValuePair<string, object>> properties
                    && properties.Any(p => p.Key == "roles" && (string)p.Value == GetStatusFunction.GetDeviceStatusRoleName));
        }

        [Fact]
        public async Task Function_ReturnsNotFound_ForMissingDocument()
        {
            var queryValues = new Dictionary<string, StringValues>
            {
                { "deviceId", "device1" }
            };

            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection(queryValues));
            request.Setup(r => r.Headers)
                .Returns(new HeaderDictionary
                {
                    [HttpRequestAuthorizationExtensions.ClientPrincipalHeaderKey] = CreatePrincipalWithRoles(GetStatusFunction.GetDeviceStatusRoleName)
                });

            var logger = new MockLogger();

            var result = await GetStatusFunction.Run(request.Object, null, logger);

            Assert.IsType<NotFoundResult>(result);
        }

        private StringValues CreatePrincipalWithRoles(params string[] roles)
        {
            var token = new JObject
            {
                ["claims"] = new JArray(roles.Select(r =>
                    new JObject
                    {
                        ["typ"] = "roles",
                        ["val"] = r
                    }).ToArray())
            };
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token.ToString()));
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
