using API.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace API.Tests.IntegrationTests
{
    public class ProductIntegrationTests : IntegrationTestBase
    {
        public ProductIntegrationTests(WebApplicationFactory<Startup> factory) : base(factory)
        {
        }

        [Fact]
        public async Task MainTest()
        {
            const string URI = "product/";

            SetAuthorization(IntegrationAuthenticationHandler.SimpleUser1);

            //create
            var c = new Product
            {
                Name = "my-name"
            };
            var res = await Client.PostAsJsonAsync(URI, c);
            res.EnsureSuccessStatusCode();        }
    }
}
