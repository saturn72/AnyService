using NUnit.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using Shouldly;
using System.Linq;
using AnyService.SampleApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using System.Net;

namespace AnyService.E2E.Authorization
{
    public class UserRoleE2ETest : E2EFixture
    {
        private static Action<IWebHostBuilder> configuration = builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var cfg = new AnyServiceConfig
                {
                    TypeConfigRecords = new[]
                    {
                        new TypeConfigRecord
                        {
                            Type = typeof(DependentModel),
                            Authorization = new AuthorizationInfo
                            {
                                ControllerAuthorizeAttribute = new AuthorizeAttribute{Roles = "some-role"}
                            }
                        }
                    }
                };
                services.AddAnyService(cfg);

                services.AddAuthentication(TestAuthHandler.AuthorizedSchemaName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthorizedSchemaName, options => { });

            });
        };

        public UserRoleE2ETest() : base(configuration)
        {
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(configuration).CreateClient();
        }

        [Test]
        public async Task CRUD_Entities_Possible_By_Authorized_User_Only()
        {
            //authorized by role client
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthorizedSchemaName);
            var uri = "dependentmodel/";
            var model = new
            {
                Value = "init value"
            };

            //create an antity
            var res = await HttpClient.PostAsJsonAsync(uri, model);
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read by creator
            res = await HttpClient.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read all by creator
            res = await HttpClient.GetAsync(uri);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();

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

            //delete by cretor
            res = await HttpClient.DeleteAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            res = await HttpClient.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["deleted"].Value<bool>().ShouldBeTrue();

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("UnAuthorized");

            var unauthRes = await HttpClient.PostAsJsonAsync(uri, model);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            unauthRes = await HttpClient.GetAsync(uri);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            unauthRes = await HttpClient.PutAsJsonAsync(uri + id, updateModel);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            unauthRes = await HttpClient.DeleteAsync(uri + id);
            unauthRes.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
