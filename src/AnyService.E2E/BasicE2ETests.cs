using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Models;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using AnyService.SampleApp.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using AnyService.SampleApp;
using System.Net.Http.Headers;

namespace AnyService.E2E
{
    public class BasicE2ETests : E2EFixture
    {
        private static Action<IWebHostBuilder> configuration = builder =>
          {
              builder.ConfigureTestServices(services =>
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
        public BasicE2ETests() : base(configuration)
        {
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(configuration).CreateClient();
        }
        [Test]
        public async Task CRUD_Dependent()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new
            {
                Value = "init value"
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync("dependentmodel", model);
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);
            #endregion
            #region read
            //read
            res = await HttpClient.GetAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read all
            //no query provided
            res = await HttpClient.GetAsync("dependentmodel/");
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            res = await HttpClient.GetAsync($"dependentmodel?query=id==\"{id}\"");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();


            throw new NotImplementedException("add getall for unpermitted entities");
            //get all public - forbid
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            var json = await HttpClient.GetStringAsync("dependentmodel/__public");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var updateModel = new
            {
                Value = "new Value"
            };
            res = await HttpClient.PutAsJsonAsync("dependentmodel/" + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //delete
            res = await HttpClient.DeleteAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task MultipartFormSampleFlow()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var multiForm = new MultipartFormDataContent();
            var filePath = Path.Combine("resources", "dog.jpg");

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

            HttpResponseMessage res;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                multiForm.Add(new StreamContent(fileStream), nameof(MultipartSampleModel.Files), Path.GetFileName(filePath));
                res = await HttpClient.PostAsync("multipartSampleModel/__multipart", multiForm);
                res.EnsureSuccessStatusCode();
                fileStream.Close();
            }
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();

            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["data"]["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
        }
        [Test]
        public async Task MultipartFormStreamSampleFlow_Create()
        {
            #region setup
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var multiForm = new MultipartFormDataContent();
            var filePath = Path.Combine("resources", "video.mp4");

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
            #endregion
            #region Create
            HttpResponseMessage res;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var fn = Path.GetFileName(filePath);
                multiForm.Add(new StreamContent(fileStream), nameof(MultipartSampleModel.Files), fn);
                res = await HttpClient.PostAsync("multipartSampleModel/__stream", multiForm);
                await Task.Delay(150);// wait for background tasks (by simulating network delay)
                res.EnsureSuccessStatusCode();
                fileStream.Close();
            }
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            #endregion
            #region Read
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["data"]["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
            #endregion
        }
        [Test]
        public async Task MultipartFormStreamSampleFlow_Update()
        {
            #region setup
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var multiForm = new MultipartFormDataContent();
            var filePath = Path.Combine("resources", "video.mp4");
            //data 
            var model = new
            {
                firstName = "Roi",
                lastName = "Shabtai"
            };
            #endregion
            #region Create
            var res = await HttpClient.PostAsJsonAsync("multipartSampleModel/", model);
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            #endregion
            #region update
            var updateModel = new
            {
                firstName = "Uriyah",
                lastName = "Shabtai Levi"
            };
            //convert data to string
            var dataString = JsonConvert.SerializeObject(updateModel);
            //add to form under key "model"
            multiForm.Add(new StringContent(dataString), "model");
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var fn = Path.GetFileName(filePath);
                multiForm.Add(new StreamContent(fileStream), nameof(MultipartSampleModel.Files), fn);
                res = await HttpClient.PutAsync("multipartSampleModel/__stream/" + id, multiForm);
            }
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            var je = jObj["data"]["entity"];
            je["id"].Value<string>().ShouldBe(id);
            je["firstName"].Value<string>().ShouldBe(updateModel.firstName);
            je["lastName"].Value<string>().ShouldBe(updateModel.lastName);
            #endregion
            #region Read
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["firstName"].Value<string>().ShouldBe(updateModel.firstName);
            (jObj["data"]["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
            #endregion
        }
        [Test]
        [Ignore("feature postpond")]
        public async Task CRUD_ControllerRouteOverwrite()
        {
            var uri = "dependent2/";
            var model = new
            {
                Value = "init value"
            };
            //create
            var res = await HttpClient.PostAsJsonAsync(uri, model);
            res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            var content = await res.Content.ReadAsStringAsync();
            res.EnsureSuccessStatusCode();
            var jObj = JObject.Parse(content);
            var id = jObj["data"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read
            res = await HttpClient.GetAsync(uri + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(model.Value);

            //read all
            res = await HttpClient.GetAsync(uri);
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
            res = await HttpClient.PutAsJsonAsync(uri + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["data"]["id"].Value<string>().ShouldBe(id);
            jObj["data"]["value"].Value<string>().ShouldBe(updateModel.Value);

            //delete
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
