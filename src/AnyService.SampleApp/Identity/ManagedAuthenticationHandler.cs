
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using ServiceStack.Text;

namespace AnyService.SampleApp.Identity
{
    public class ManagedAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string RolePrefix = "role_";
        private const string Delimiter = "__";
        public const string Schema = "Test";
        public const string AuthorizedJson = RolePrefix + "some-role";
        public const string UnauthorizedJson = RolePrefix + "unauth-role";

        public ManagedAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Request.Headers.TryGetValue("Authorization", out StringValues authData);
            var raw = authData.ToString().Split(Delimiter, StringSplitOptions.RemoveEmptyEntries);

            var claims = new List<Claim>();
            foreach (var r in raw.Where(x => x.StartsWith(RolePrefix)))
                claims.Add(new Claim(ClaimTypes.Role, r.Replace(RolePrefix, "")));

            var identity = new ClaimsIdentity(claims, Schema);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Schema);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}