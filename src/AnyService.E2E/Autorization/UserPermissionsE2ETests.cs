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
using Microsoft.AspNetCore.Authorization;

namespace AnyService.E2E.Authorization
{
    public class UserPermissionsE2ETest : E2EFixture
    {
        private static Action<IWebHostBuilder> configuration = builder =>
        {
            builder.ConfigureServices(services =>
            {
                throw new System.NotImplementedException();
                // services.AddMvc(o => o.EnableEndpointRouting = false);
                // var cfg = new AnyServiceConfig
                // {
                //     TypeConfigRecords = new[]
                //     {
                //             new TypeConfigRecord
                //             {
                //                 Type = typeof(DependentModel),
                //                 AuthorizeAttribute = new AuthorizeAttribute
                //                 {
                //                     Roles = "editor"
                //                 }
                //             }
                //     }
                // };
                // services.AddAnyService(cfg);
            });
        };

        public UserPermissionsE2ETest() : base(configuration)
        {
        }

        [Test]
        public async Task UserPermissionsE2ETests()
        {
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

            throw new NotImplementedException("read by another user - declined");
            //read all by creator
            res = await HttpClient.GetAsync(uri);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();
            throw new NotImplementedException("read all by another user - declined");

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
            throw new NotImplementedException("update all by another user - declined");

            throw new NotImplementedException("delete all by another user - declined");

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
        }
    }
}