﻿using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shouldly;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using AnyService.SampleApp.Models;
using System.Net;
using System.Linq;
using AnyService.SampleApp.Entities;

namespace AnyService.E2E
{

    public class MapToTypeTests : E2ETestBase
    {
        private const string URI = "category";
        public MapToTypeTests(E2EFixture fixture, ITestOutputHelper outputHelper) :
            base(fixture, outputHelper)
        {
        }

        [Fact]
        public async Task CRUD_Category()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new CategoryModel
            {
                CategoryName = "cat-name",
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync(URI, model);
            res.EnsureSuccessStatusCode();
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["categoryName"].Value<string>().ShouldBe(model.CategoryName);
            #endregion

            #region read
            //read
            res = await HttpClient.GetAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["categoryName"].Value<string>().ShouldBe(model.CategoryName);

            //no query provided
            res = await HttpClient.GetAsync($"{URI}/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            //get all with projection
            res = await HttpClient.GetAsync($"{URI}?projectedFields=categoryName");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            
            //var jArr = JArray.Parse(content);
            //jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            //jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();



            res = await HttpClient.GetAsync($"{URI}?query=id==\"{id}\"");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            var jArr = JArray.Parse(content);
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();

            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            model.CategoryName = "new name";
            res = await HttpClient.PutAsJsonAsync($"{URI}/{id}", model);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["categoryName"].Value<string>().ShouldBe(model.CategoryName);

            //delete
            res = await HttpClient.DeleteAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["categoryName"].Value<string>().ShouldBe(model.CategoryName);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync($"{URI}/{id}");
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CRUD_AdminCategory()
        {
            const string AdminUri = "admiN/category";

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new Category
            {
                Name = "cat-name",
                AdminComment = "admin-comment"
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync(AdminUri, model);
            res.EnsureSuccessStatusCode();
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var id = jObj["id"].Value<string>();
            id.ShouldNotBeNullOrEmpty();
            jObj["name"].Value<string>().ShouldBe(model.Name);
            jObj["adminComment"].Value<string>().ShouldBe(model.AdminComment);
            #endregion

            #region read
            //read
            res = await HttpClient.GetAsync($"{AdminUri}/{id}");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["name"].Value<string>().ShouldBe(model.Name);
            jObj["adminComment"].Value<string>().ShouldBe(model.AdminComment);

            //no query provided
            res = await HttpClient.GetAsync($"{AdminUri}/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"{AdminUri}?query=id==\"{ id}\"");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            var jArr = JArray.Parse(content);
            jArr.Count.ShouldBeGreaterThanOrEqualTo(1);
            jArr.Any(x => x["id"].Value<string>() == id).ShouldBeTrue();
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            model.Name = "new name";
            res = await HttpClient.PutAsJsonAsync($"{AdminUri}/{id}", model);
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["name"].Value<string>().ShouldBe(model.Name);
            jObj["adminComment"].Value<string>().ShouldBe(model.AdminComment);

            //delete
            res = await HttpClient.DeleteAsync($"{AdminUri}/{id}");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jObj["id"].Value<string>().ShouldBe(id);
            jObj["name"].Value<string>().ShouldBe(model.Name);
            jObj["adminComment"].Value<string>().ShouldBe(model.AdminComment);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync($"{AdminUri}/{id}");
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
