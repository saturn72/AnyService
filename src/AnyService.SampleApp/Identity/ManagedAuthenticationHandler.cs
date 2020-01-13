
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
        public const string AuthorizedSchemaName = "Authorized";
        public ManagedAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = !Request.Headers.TryGetValue("Authorization", out StringValues value) && value == AuthorizedSchemaName ? "some-role" : "another-role";

            var claims = new[] { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}