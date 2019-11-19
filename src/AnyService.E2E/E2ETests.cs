using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;

namespace AnyService.E2E
{
    public class E2ETests : WebApplicationFactoryFixture
    {
        [Test]
        public async Task CRUD_Dependent()
        {
            var model = new
            {
                Value = "init value"
            };

            //create
            var res = await Client.PostAsJsonAsync("dependentmodel", model);
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read
            res = await Client.GetAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read all
            res = await Client.GetAsync("dependentmodel/");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();

            //update
            var updateModel = new
            {
                Value = "new Value"
            };
            res = await Client.PutAsJsonAsync("dependentmodel/" + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //delete
            res = await Client.DeleteAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            res = await Client.GetAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["deleted"].Value<bool>().ShouldBeTrue();
        }

        [Test]
        public async Task MultipartFormSampleFlow()
        {
            var multiForm = new MultipartFormDataContent();
            var filePath = Path.Combine(AppContext.BaseDirectory, "resources", "dog.jpg");

            //data 
            var model = new
            {
                firstName = "Roi",
                lastName = "Shabtai"
            };
            //convert data to string
            var dataString = JsonConvert.SerializeObject(model);
            //add to form under key "model"
            multiForm.Add(new StringContent(dataString), "model");

            var fileStream = new FileStream(filePath, FileMode.Open);
            multiForm.Add(new StreamContent(fileStream), nameof(MultipartSampleModel.Files), Path.GetFileName(filePath));
            var res = await Client.PostAsync("multipartSampleModel/__multipart", multiForm);
            res.EnsureSuccessStatusCode();

            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();

            res = await Client.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["data"]["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
        }
        [Test]
        public async Task MultipartFormStreamSampleFlow()
        {
            var multiForm = new MultipartFormDataContent();
            var filePath = Path.Combine(AppContext.BaseDirectory, "resources", "video.mp4");

            //data 
            var model = new
            {
                firstName = "Roi",
                lastName = "Shabtai"
            };
            //convert data to string
            var dataString = JsonConvert.SerializeObject(model);
            //add to form under key "model"
            multiForm.Add(new StringContent(dataString), "model");

            var fileStream = new FileStream(filePath, FileMode.Open);
            multiForm.Add(new StreamContent(fileStream), nameof(MultipartSampleModel.Files), Path.GetFileName(filePath));
            var res = await Client.PostAsync("multipartSampleModel/__stream", multiForm);
            res.EnsureSuccessStatusCode();

            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();

            res = await Client.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["data"]["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
        }
    }
}
