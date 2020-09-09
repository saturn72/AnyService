using API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;

namespace API.Tests.IntegrationTests
{
    public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Startup>>
    {
        protected readonly WebApplicationFactory<Startup> Factory;
        protected readonly HttpClient Client;
        public IntegrationTestBase(WebApplicationFactory<Startup> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddAuthentication(o =>
                        {
                            o.DefaultScheme = IntegrationAuthenticationHandler.Schema;
                            o.DefaultAuthenticateScheme = IntegrationAuthenticationHandler.Schema;
                        }).AddScheme<AuthenticationSchemeOptions, IntegrationAuthenticationHandler>(IntegrationAuthenticationHandler.Schema, options => { });

                        ConfigureEntityFramework(services);

                    });
                });

            Client = Factory.CreateClient();
        }
        private void ConfigureEntityFramework(IServiceCollection services)
        {
            var dbName = "prog-tool.db";
            var options = new DbContextOptionsBuilder<JanusDbContext>()
                .UseInMemoryDatabase(databaseName: dbName).Options;
            services.AddTransient<DbContext>(sp => new JanusDbContext(options));

        }

        protected void SetAuthorization(string user)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IntegrationAuthenticationHandler.Schema, user);
        }
    }
}
