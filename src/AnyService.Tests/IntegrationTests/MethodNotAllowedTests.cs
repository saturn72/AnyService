using AnyService.SampleApp;
using AnyService.SampleApp.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.IntegrationTests
{
    public class MethodNotAllowedTests : IntegrationTestsBase
    {
        public MethodNotAllowedTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task PostMethodNotAllowed()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new
            {
                value = "dddd"
            };
            var res = await Client.PostAsJsonAsync("api/na", model);
            res.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        }
        [Fact]
        public async Task PutMethodNotAllowed()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new
            {
                value = "dddd"
            };
            var res = await Client.PutAsJsonAsync("api/na", model);
            res.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        }
        [Fact]
        public async Task DeleteMethodNotAllowed()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new
            {
                value = "dddd"
            };
            var res = await Client.DeleteAsync("api/na/asd");
            res.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        }
    }
}
