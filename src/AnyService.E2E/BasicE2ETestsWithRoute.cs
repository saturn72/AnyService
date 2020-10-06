using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shouldly;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using AnyService.SampleApp.Models;

namespace AnyService.E2E
{
    public class BasicE2ETestsWithRoute : E2EFixture
    {
        private const string URI = "api/d";
        public BasicE2ETestsWithRoute(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        [Fact]
        public async Task CRUD_WithNonDefaultRoute()
        {
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new Dependent2
            {
                Value = "init value"
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync(URI, model);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadAsAsync<Dependent2>();
            var id = d.Id;
            id.ShouldNotBeNullOrEmpty();
            d.Value.ShouldBe(model.Value);
            await Task.Delay(250); //simulate network ltency
            #endregion
            #region read
            //read
            res = await HttpClient.GetAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            d = await res.Content.ReadAsAsync<Dependent2>();
            d.Id.ShouldBe(id);
            d.Value.ShouldBe(model.Value);

            //no query provided
            res = await HttpClient.GetAsync(URI);
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"{URI}?query=id==\"{id}\"");
            res.EnsureSuccessStatusCode();
            var dArr = await res.Content.ReadAsAsync<Dependent2[]>();
            dArr.Length.ShouldBeGreaterThanOrEqualTo(1);
            dArr.ShouldContain(x => x.Id == id);
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var updateModel = new
            {
                Value = "new Value"
            };
            res = await HttpClient.PutAsJsonAsync($"{URI}/{id}", updateModel);
            res.EnsureSuccessStatusCode();
            d = await res.Content.ReadAsAsync<Dependent2>();
            d.Id.ShouldBe(id);
            d.Value.ShouldBe(updateModel.Value);

            //delete
            res = await HttpClient.DeleteAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            d = await res.Content.ReadAsAsync<Dependent2>();
            d.Id.ShouldBe(id);
            d.Value.ShouldBe(updateModel.Value);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync($"{URI}/{id}");
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
