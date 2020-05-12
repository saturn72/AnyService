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
using Microsoft.EntityFrameworkCore;

namespace AnyService.E2E
{
    public class BasicE2ETests : E2EFixture
    {
        static SampleAppDbContext _ctx;
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
                  var options = new DbContextOptionsBuilder<SampleAppDbContext>()
                    .UseInMemoryDatabase(databaseName: DateTime.Now.ToString("yyyy_mm_hh_ss_ff") + ".db").Options;
                  _ctx = new SampleAppDbContext(options);

                  services.AddTransient<DbContext>(sp => new SampleAppDbContext(options));
              });
          };
        public BasicE2ETests() : base(configuration)
        {
            Factory = new WebApplicationFactory<Startup>();
            HttpClient = Factory.WithWebHostBuilder(configuration).CreateClient();
        }
        [Test]
        public async Task Read_ReservedQueries()
        {
            var totalEntities = 6;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new DependentModel
            {
                Value = "init value",
                Public = false
            };
            for (int i = 0; i < totalEntities; i++)
            {
                model.Public = i % 2 == 0;
                var r = await HttpClient.PostAsJsonAsync("dependentmodel", model);
                r.EnsureSuccessStatusCode();
            }

            #region create
            var res = await HttpClient.GetAsync($"dependentmodel?query=__created");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"dependentmodel?query=__created");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region public
            res = await HttpClient.GetAsync($"dependentmodel?query=__public");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities / 2);
            #endregion 
            #region updated
            var id1 = jArr.ElementAt(0).Value<string>("id");
            var id2 = jArr.ElementAt(1).Value<string>("id");

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var updateModel = new DependentModel
            {
                Value = "updated-value",
                Public = false
            };
            res = await HttpClient.PutAsJsonAsync($"dependentmodel/{id1}", updateModel);
            res.EnsureSuccessStatusCode();
            res = await HttpClient.GetAsync($"dependentmodel?query=__updated");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(1);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.PutAsJsonAsync($"dependentmodel/{id1}", updateModel);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__updated");
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region delete
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            res = await HttpClient.DeleteAsync($"dependentmodel/{id1}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.DeleteAsync($"dependentmodel/{id2}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.GetAsync($"dependentmodel?query=__deleted");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(2);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);

            res = await HttpClient.DeleteAsync($"dependentmodel/{id1}");
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            res = await HttpClient.DeleteAsync($"dependentmodel/{id2}");
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__deleted");
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region canRead
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canRead");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canRead");
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion

            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canUpdate");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canUpdate");
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canDelete");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canDelete");
            jObj = JObject.Parse(content);
            jArr = jObj["data"]["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
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
    }
}
