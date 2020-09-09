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
        public async Task CRUD_Product()
        {
            const string URI = "admin/product/";

            SetAuthorization(IntegrationAuthenticationHandler.SimpleUser1);

            //create
            var c = new Product
            {
                Name = "my-name"
            };
            var res = await Client.PostAsJsonAsync(URI, c);
            res.EnsureSuccessStatusCode();

            var createdProduct = await res.Content.ReadAsAsync<Product>();
            var pId = createdProduct.Id;

            //get by id
            res = await Client.GetAsync($"{URI}{pId}");
            res.EnsureSuccessStatusCode();
            var getProduct = await res.Content.ReadAsAsync<Product>();
            getProduct.Name.ShouldBe(createdProduct.Name);

            //update
            c.Name = "new-name";
            res = await Client.PutAsJsonAsync($"{URI}{pId}", c);
            res.EnsureSuccessStatusCode();
            var updatedProduct = await res.Content.ReadAsAsync<Product>();
            updatedProduct.Name.ShouldBe(c.Name);

            //delete
            res = await Client.DeleteAsync($"{URI}{pId}");
            res.EnsureSuccessStatusCode();
            var deletedProduct = await res.Content.ReadAsAsync<Product>();
            deletedProduct.Id.ShouldBe(createdProduct.Id);

            //get deleted - returns bad request
            res = await Client.GetAsync($"{URI}{pId}");
            res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }
}
