using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DroneStatusFunctionApp
{
    public static class ClaimsPrincipalAuthorizationExtensions
    {
        public static bool IsAuthorizedByRoles(
            this ClaimsPrincipal principal,
            string[] roles,
            ILogger log)
        {
            var principalRoles = new HashSet<string>(principal.Claims.Where(kvp => kvp.Type == "roles").Select(kvp => kvp.Value));
            var missingRoles = roles.Where(r => !principalRoles.Contains(r)).ToArray();
            if (missingRoles.Length > 0)
            {
                log.LogWarning("The principal does not have the required {roles}", string.Join(", ", missingRoles));
                return false;
            }

            return true;
        }
    }
}
