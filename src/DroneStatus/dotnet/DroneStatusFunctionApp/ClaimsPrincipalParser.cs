using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

//https://learn.microsoft.com/azure/app-service/configure-authentication-user-identities#access-user-claims-in-app-code
namespace DroneStatusFunctionApp
{
    public static class ClaimsPrincipalParser
    {
        private class ClientPrincipalClaim
        {
            [JsonPropertyName("typ")]
            public string Type { get; set; }
            [JsonPropertyName("val")]
            public string Value { get; set; }
        }

        private class ClientPrincipal
        {
            [JsonPropertyName("auth_typ")]
            public string IdentityProvider { get; set; }
            [JsonPropertyName("name_typ")]
            public string NameClaimType { get; set; }
            [JsonPropertyName("role_typ")]
            public string RoleClaimType { get; set; }
            [JsonPropertyName("claims")]
            public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
        }

        public static ClaimsPrincipal Parse(HttpRequest req)
        {
            var principal = new ClientPrincipal();

            if (req.Headers.TryGetValue("x-ms-client-principal", out var header))
            {
                var data = header[0];
                var decoded = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(decoded);
                principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var identity = new ClaimsIdentity(principal.IdentityProvider, principal.NameClaimType, principal.RoleClaimType);
                identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));

                return new ClaimsPrincipal(identity);
            }

            return null;
        }
    }
}
