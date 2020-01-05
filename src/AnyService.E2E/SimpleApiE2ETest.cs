using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Shouldly;
using System.Linq;
using AnyService.SampleApp.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using Microsoft.AspNetCore.Hosting;

namespace AnyService.E2E
{
    public class SimpleApiE2ETest : E2EFixture
    {
        private static Action<IWebHostBuilder> configuration = builder =>
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
             };
        public SimpleApiE2ETest() : base(configuration)
        { }

        [Test]
        public async Task RunTest()
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
        }
    }
}