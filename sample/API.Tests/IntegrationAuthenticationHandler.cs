using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace API.Tests
{
    public class IntegrationAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Schema = "int-auth-schema";

        public const string SimpleUser1 = "system-admin-user-id";
        public IntegrationAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Request.Headers.TryGetValue("Authorization", out StringValues authHeader);
            var token = authHeader[0].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var identity = new ClaimsIdentity(Tokens[token], Schema);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Schema);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
        private static readonly IReadOnlyDictionary<string, IEnumerable<Claim>> Tokens = new Dictionary<string, IEnumerable<Claim>>
        {
              {
                SimpleUser1,
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, SimpleUser1),
                    //new Claim(ClaimTypes.Role, "communit-create"),
                    //new Claim(ClaimTypes.Role, "communit-read"),
                    //new Claim(ClaimTypes.Role, "communit-update"),
                    //new Claim(ClaimTypes.Role, "communit-delete"),
                }
            },
        };

    }
}
