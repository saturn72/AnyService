using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Shouldly;
using AnyService.SampleApp.Models;

namespace AnyService.E2E
{
    public class CustomControllerTests : E2EFixture
    {
        public CustomControllerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task PostNewValue_ToCustomController()
        {
            var model = new
            {
                Value = "ping"
            };

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var res = await HttpClient.PostAsJsonAsync("v1/my-great-route", model);
            res.EnsureSuccessStatusCode();
            var c = await res.Content.ReadAsStringAsync();
            c.ShouldBe("pong");
        }

        [Fact]
        public async Task PostNewValue_ToOverridenController()
        {
            var model = new CustomModel
            {
                Value = "ping"
            };

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var res = await HttpClient.PostAsJsonAsync("CustomModel", model);
            res.EnsureSuccessStatusCode();
            var c = await res.Content.ReadAsStringAsync();
            c.ShouldNotBe("pong");
        }
    }
}
