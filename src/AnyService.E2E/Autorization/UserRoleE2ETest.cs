using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Shouldly;
using System.Net.Http.Headers;
using System.Net;
using AnyService.SampleApp.Identity;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E.Authorization
{
    public class UserRoleE2ETest : E2EFixture
    {
        public UserRoleE2ETest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task CRUD_Entities_Possible_By_Authorized_User_Only()
        {
            #region authorized user
            //authorized by role client
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var uri = "dependentmodel/";
            var model = new
            {
                Value = "init value"
            };

            //create an entity
            var res = await HttpClient.PostAsJsonAsync(uri, model);
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read by creator
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //update by cretor
            var updateModel = new
            {
                Value = "new Value"
            };
            res = await HttpClient.PutAsJsonAsync(uri + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);
            #endregion
            //un authorized requests

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.UnauthorizedUser1);
            var unauthRes = await HttpClient.GetAsync(uri + id);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            unauthRes = await HttpClient.PutAsJsonAsync(uri + id, updateModel);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            unauthRes = await HttpClient.DeleteAsync(uri + id);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }
}
