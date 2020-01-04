using NUnit.Framework;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using Shouldly;
using System.Linq;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using AnyService.SampleApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.E2E
{
    public class SimpleApiE2ETest : IClassFixture<WebApplicationFactory<Startup>>
    {
        [Fact]
        public void RunTest()
        {
            throw nameof Syatem.NotImplementedException("Perform the simplest configuration for any service")
        }

    }
    public class UserPermissionsE2ETest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;
        public UserPermissionsE2ETest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddMvc(o => o.EnableEndpointRouting = false);
                    var entities = new[]
                    {
                    typeof(DependentModel),
                    typeof(Dependent2),
                    typeof(MultipartSampleModel)
                    };
                    services.AddAnyService(entities);
                });
            }).CreateClient();
        }
        [Fact]
        public async Task UserPermissionsE2ETests()
        {
            var uri = "dependentmodel/";
            var model = new
            {
                Value = "init value"
            };

            //create an antity
            var res = await _client.PostAsJsonAsync(uri, model);
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read by creator
            res = await _client.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            throw new NotImplementedException("read by another user - declined");
            //read all by creator
            res = await _client.GetAsync(uri);
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
            res = await _client.PutAsJsonAsync(uri + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);
            throw new NotImplementedException("update all by another user - declined");

            throw new NotImplementedException("delete all by another user - declined");

            //delete by cretor
            res = await _client.DeleteAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            res = await _client.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["deleted"].Value<bool>().ShouldBeTrue();
        }
    }
}