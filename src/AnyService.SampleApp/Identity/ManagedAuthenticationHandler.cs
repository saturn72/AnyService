
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

namespace AnyService.SampleApp.Identity
{
    public class ManagedAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string RolePrefix = "role_";
        private const string UserIdPrefix = "id_";
        private const string Delimiter = "__";
        public const string Schema = "Test";
        public const string AuthorizedJson1 = RolePrefix + "some-role" + Delimiter + UserIdPrefix + "1";
        public const string AuthorizedJson2 = RolePrefix + "some-role" + Delimiter + UserIdPrefix + "2";
        public const string AuthorizedJson3 = RolePrefix + "some-role" + Delimiter + UserIdPrefix + "3";
        public const string UnauthorizedUser1 = RolePrefix + "unauthz-role" + Delimiter + UserIdPrefix + "3";

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

            var userId = raw.FirstOrDefault(x => x.StartsWith(UserIdPrefix));
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Replace(UserIdPrefix, "")));

            var identity = new ClaimsIdentity(claims, Schema);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Schema);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}