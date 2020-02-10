using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;
using System;

namespace AnyService
{
    public sealed class AlwaysPassAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Schema = "anyservice-auth-schema";
        public static string UserId { get; set; }
        public static IEnumerable<Claim> Claims { get; set; }

        public AlwaysPassAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                  ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                  : base(options, logger, encoder, clock)
        {
        }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authClaims = new List<Claim>();

            if (Claims != null && Claims.Any())
                authClaims.AddRange(Claims);

            if (UserId != null)
                authClaims.Add(new Claim(ClaimTypes.NameIdentifier, UserId));

            var identity = new ClaimsIdentity(authClaims, Schema);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Schema);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }

    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAlwaysPassAuthentication(this IServiceCollection services, string userId, IEnumerable<KeyValuePair<string, string>> claims)
        {
            if (!userId.HasValue())
                throw new InvalidOperationException("userId is mandatory");

            AlwaysPassAuthenticationHandler.UserId = userId;

            if (claims != null && claims.Any())
                AlwaysPassAuthenticationHandler.Claims = claims.Select(x => new Claim(x.Key, x.Value)).ToArray();

            services.AddAuthentication(AlwaysPassAuthenticationHandler.Schema)
             .AddScheme<AuthenticationSchemeOptions, AlwaysPassAuthenticationHandler>(
                 AlwaysPassAuthenticationHandler.Schema, options => { });
            return services;
        }
    }
}