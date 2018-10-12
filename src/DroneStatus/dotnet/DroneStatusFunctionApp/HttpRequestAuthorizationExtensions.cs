using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneStatusFunctionApp
{
    public static class HttpRequestAuthorizationExtensions
    {
        public const string ClientPrincipalHeaderKey = "X-MS-CLIENT-PRINCIPAL";

        public static Task<IActionResult> HandleIfAuthorizedForRoles(
            this HttpRequest request, string[] roles, Func<Task<IActionResult>> handler, ILogger log)
        {
            return request.HandleIfAuthorizedByClaims(
                (claims, _) =>
                {
                    var principalRoles = new HashSet<string>(claims.Where(kvp => kvp.typ == "roles").Select(kvp => kvp.val));
                    var missingRoles = roles.Where(r => !principalRoles.Contains(r)).ToArray();
                    if (missingRoles.Length > 0)
                    {
                        log.LogWarning("The principal does not have the required {roles}", string.Join(", ", missingRoles));
                        return false;
                    }

                    return true;
                },
                handler,
                log);
        }

        public static async Task<IActionResult> HandleIfAuthorizedByClaims(
            this HttpRequest request,
            Func<IEnumerable<(string typ, string val)>, ILogger, bool> authorizeClaims,
            Func<Task<IActionResult>> handler,
            ILogger log)
        {
            return request.GetResultIfUnauthorized(authorizeClaims, log) ?? await handler();
        }

        public static IActionResult GetResultIfUnauthorized(
            this HttpRequest request,
            Func<IEnumerable<(string typ, string val)>, ILogger, bool> authorizeClaims,
            ILogger log)
        {
            if (!request.Headers.TryGetValue(ClientPrincipalHeaderKey, out var values))
            {
                log.LogError("The request does not contain the required header {header}", ClientPrincipalHeaderKey);
                return new UnauthorizedResult();
            }

            IEnumerable<(string typ, string val)> claims;
            try
            {
                if (!(values.ToArray() is var principals
                    && principals.Length == 1
                    && JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(principals[0]))) is var token
                    && token["claims"] is JArray claimsArray
                    && (claims = claimsArray.Select(c => (typ: c["typ"].Value<string>(), val: c["val"].Value<string>()))) != null))
                {
                    log.LogError("The value of header {header} does not contain the expected information", ClientPrincipalHeaderKey);
                    return new UnauthorizedResult();
                }
            }
            catch (FormatException)
            {
                log.LogError("The value of header {header} does not have the expected format", ClientPrincipalHeaderKey);
                return new UnauthorizedResult();
            }

            if (authorizeClaims(claims, log))
            {
                return null;
            }

            return new UnauthorizedResult();
        }
    }
}
