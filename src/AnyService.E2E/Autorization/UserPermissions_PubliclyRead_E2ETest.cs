using NUnit.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using Shouldly;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using AnyService.SampleApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.TestHost;
using AnyService.SampleApp.Identity;
using System.Net;

namespace AnyService.E2E.Authorization
{
    public class UserPermissions_PubliclyRead_E2ETest : E2EFixture
    {
        private static Action<IWebHostBuilder> configuration = builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var cfg = new AnyServiceConfig
                {
                    ManageEntityPermissions = true,
                    EntityConfigRecords = new[]
                    {
                        new EntityConfigRecord
                        {
                            Type = typeof(DependentModel),
                            PublicGet = true,
                            Authorization = new AuthorizationInfo
                            {
                                ControllerAuthorizationNode = new AuthorizationNode{Roles = new[]{"some-role"}}
                            }
                        }
                    }
                };
                services.AddAnyService(cfg);
            });
        };

        public UserPermissions_PubliclyRead_E2ETest() : base(configuration)
        {
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(configuration).CreateClient();
        }

        [Test]
        public async Task UserPermissions_CUD_By_Creator_Read_by_All_Users()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var uri = "dependentmodel/";
            var model = new
            {
                Value = "init value"
            };

            //create an antity
            var res = await HttpClient.PostAsJsonAsync(uri, model);
            await Task.Delay(150); // wait for background tasks (by simulating network delay)

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

            //switch user
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync(uri);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            res = await HttpClient.PutAsJsonAsync(uri + id, updateModel);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            //delete by another user - declined
            res = await HttpClient.DeleteAsync(uri + id);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            //read by non-creator
            res = await HttpClient.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read all by non-creator
            res = await HttpClient.GetAsync(uri);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();

            //delete by cretor
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            res = await HttpClient.DeleteAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //switch user and get deleted
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync(uri);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }
}