using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Shouldly;

namespace AnyService.E2E
{
    public class CustomControllerTests : E2ETestBase
    {
        private const string URI = "v1/my-great-route";
        public CustomControllerTests(E2EFixture fixture, ITestOutputHelper outputHelper) :
            base(fixture, outputHelper)
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
          
            var res = await HttpClient.PostAsJsonAsync(URI, model);
            res.EnsureSuccessStatusCode();
            var c = await res.Content.ReadAsStringAsync();
            c.ShouldBe("pong");
        }
    }
}
