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

namespace AnyService.E2E
{
    public class BasicE2ETestsWithRoute : E2EFixture
    {
        public BasicE2ETestsWithRoute(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        [Fact]
        public async Task CRUD_WithNonDefaultRoute()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new
            {
                Value = "init value"
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync("api/d/", model);
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["value"].Value<string>().ShouldBe(model.Value);
            #endregion
            #region read
            //read
            res = await HttpClient.GetAsync("api/d/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(model.Value);

            //no query provided
            res = await HttpClient.GetAsync("api/d/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"api/d?query=id==\"{id}\"");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var updateModel = new
            {
                Value = "new Value"
            };
            res = await HttpClient.PutAsJsonAsync("api/d/" + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(updateModel.Value);

            //delete
            res = await HttpClient.DeleteAsync("api/d/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync("api/d/" + id);
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
