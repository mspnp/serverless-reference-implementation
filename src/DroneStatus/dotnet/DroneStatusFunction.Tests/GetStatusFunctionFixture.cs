using DroneStatusFunctionApp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Drawing.Imaging;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

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

            var clientPrincipal = new
            {
                auth_typ = "test",
                name_typ = "test",
                role_typ = "test",
                claims = new[]
                    {
                        new
                        {
                            typ = "roles",
                            val = "GetStatus" 
                        }
                    }
            };

            var json = JsonSerializer.Serialize(clientPrincipal);
            var bytes = Encoding.UTF8.GetBytes(json);
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary
            {
                { "x-ms-client-principal", encoded }
            };
            
            request.SetupGet(r => r.Headers).Returns(headers);

            var logger = new MockLogger();
            var getStatusFunction = new GetStatusFunction(logger);

            var result = getStatusFunction.Run  (request.Object, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Function_ReturnsUnauthorized_ForPrincipalWithoutRoles()
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());

            var headers = new HeaderDictionary();

            request.SetupGet(r => r.Headers).Returns(headers);

            var logger = new MockLogger();
            var getStatusFunction = new GetStatusFunction(logger);

            var result = getStatusFunction.Run(request.Object, null);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public void Function_ReturnsUnauthorized_ForPrincipalWithNonRequiredRoles()
        {
            var request = new Mock<HttpRequest>();
            var mockLogger = new Mock<ILogger<GetStatusFunction>>();
            request.SetupGet(r => r.Query)
                .Returns(new QueryCollection());


            var clientPrincipal = new
            {
                auth_typ = "test",
                name_typ = "test",
                role_typ = "test",
                claims = new[]
                    {
                        new
                        {
                            typ = "roles",
                            val = "SomeRole"
                        },
                         new
                        {
                            typ = "roles",
                            val = "SomeRoleOtherRole"
                        }
                    }
            };

            var json = JsonSerializer.Serialize(clientPrincipal);
            var bytes = Encoding.UTF8.GetBytes(json);
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary
            {
                { "x-ms-client-principal", encoded }
            };

            request.SetupGet(r => r.Headers).Returns(headers);

            var logger = new MockLogger();
            var getStatusFunction = new GetStatusFunction(logger);

            var result = getStatusFunction.Run(request.Object, null);

            Assert.IsType<UnauthorizedResult>(result);
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

            var clientPrincipal = new
            {
                auth_typ = "test",
                name_typ = "test",
                role_typ = "test",
                claims = new[]
                     {
                        new
                        {
                            typ = "roles",
                            val = "GetStatus"
                        }
                    }
            };

            var json = JsonSerializer.Serialize(clientPrincipal);
            var bytes = Encoding.UTF8.GetBytes(json);
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary
            {
                { "x-ms-client-principal", encoded }
            };
            request.SetupGet(r => r.Headers).Returns(headers);

            var logger = new MockLogger();
            var getStatusFunction = new GetStatusFunction(logger);

            var result = getStatusFunction.Run(request.Object, null);

            Assert.IsType<NotFoundResult>(result);
        }

        private class MockLogger : ILogger<GetStatusFunction>
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
