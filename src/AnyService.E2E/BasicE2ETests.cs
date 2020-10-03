using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E
{

    public class BasicE2ETests : E2EFixture
    {
        public BasicE2ETests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        [Fact]
        public async Task ReadsOnlyPermittedEntries()
        {
            var users = new[]{
                ManagedAuthenticationHandler.AuthorizedJson1,
                ManagedAuthenticationHandler.AuthorizedJson2
            };
            var totalEntitiesPerUser = 6;
            var model = new DependentModel
            {
                Value = "some-value",
                Public = false
            };
            foreach (var usr in users)
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(usr);
                for (var i = 0; i < totalEntitiesPerUser; i++)
                {
                    var r = await HttpClient.PostAsJsonAsync("dependentmodel", model);

                    r.EnsureSuccessStatusCode();
                }
            }
            var c = await HttpClient.GetStringAsync($"dependentmodel?query=value ==\"" + model.Value + "\"");
            var jObj = JObject.Parse(c);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntitiesPerUser);
        }
        [Fact]
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
            res.EnsureSuccessStatusCode();
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["value"].Value<string>().ShouldBe(model.Value);
            #endregion
            #region read
            //read
            res = await HttpClient.GetAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(model.Value);

            //no query provided
            res = await HttpClient.GetAsync("dependentmodel/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"dependentmodel?query=id==\"{id}\"");
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
            res = await HttpClient.PutAsJsonAsync("dependentmodel/" + id, updateModel);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(updateModel.Value);

            //delete
            res = await HttpClient.DeleteAsync("dependentmodel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["value"].Value<string>().ShouldBe(updateModel.Value);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync("dependentmodel/" + id);
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        }
        [Fact(Skip = "file upload not supported at this point")]
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
            var id = jObj["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();

            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
        }
        [Fact(Skip = "inconsist result")]
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
            var id = jObj["entity"]["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            #endregion
            #region Read
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["firstName"].Value<string>().ShouldBe(model.firstName);
            (jObj["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
            #endregion
        }
        [Fact(Skip = "file upload not supported at this point")]
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
            var id = jObj["id"].Value<string>();
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
            var je = jObj["entity"];
            je["id"].Value<string>().ShouldBe(id);
            je["firstName"].Value<string>().ShouldBe(updateModel.firstName);
            je["lastName"].Value<string>().ShouldBe(updateModel.lastName);
            #endregion
            #region Read
            res = await HttpClient.GetAsync("multipartSampleModel/" + id);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["firstName"].Value<string>().ShouldBe(updateModel.firstName);
            (jObj["files"] as JArray).First["parentId"].Value<string>().ShouldBe(id);
            #endregion
        }
    }
}
