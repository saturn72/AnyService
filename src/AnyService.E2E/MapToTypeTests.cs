using System.Net.Http;
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

    public class MapToTypeTests : E2EFixture
    {
        private const string URI = "category";
        public MapToTypeTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task CRUD_Category()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            var model = new CategoryModel
            {
                Name = "cat-name",
            };

            #region create
            //create
            var res = await HttpClient.PostAsJsonAsync(URI, model);
            res.EnsureSuccessStatusCode();
            await Task.Delay(150);// wait for background tasks (by simulating network delay)
            var cm = await res.Content.ReadAsAsync<CategoryModel>();
            var id = cm.Id;
            id.ShouldNotBeNullOrEmpty();
            cm.Name.ShouldBe(model.Name);
            #endregion

            #region read
            //read
            res = await HttpClient.GetAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<CategoryModel>();
            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);

            //no query provided
            res = await HttpClient.GetAsync($"{URI}/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"{URI}?query=id==\"{ id}\"");
            res.EnsureSuccessStatusCode();
            var cmArr = await res.Content.ReadAsAsync<CategoryModel[]>();
            cmArr.Length.ShouldBeGreaterThanOrEqualTo(1);
            cmArr.ShouldContain(x => x.Id == id);
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            model.Name = "new name";
            res = await HttpClient.PutAsJsonAsync($"{URI}/{id}", model);
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<CategoryModel>();

            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);

            //delete
            res = await HttpClient.DeleteAsync($"{URI}/{id}");
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<CategoryModel>();

            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);

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
            var cm = await res.Content.ReadAsAsync<Category>();

            var id = cm.Id;
            id.ShouldNotBeNullOrEmpty();
            cm.Name.ShouldBe(model.Name);
            cm.AdminComment.ShouldBe(model.AdminComment);
            #endregion

            #region read
            //read
            res = await HttpClient.GetAsync($"{AdminUri}/{id}");
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<Category>();

            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);
            cm.AdminComment.ShouldBe(model.AdminComment);

            //no query provided
            res = await HttpClient.GetAsync($"{AdminUri}/");
            res.StatusCode.ShouldBe(HttpStatusCode.OK);

            res = await HttpClient.GetAsync($"{AdminUri}?query=id==\"{ id}\"");
            res.EnsureSuccessStatusCode();
            var cmArr = await res.Content.ReadAsAsync<Category[]>();
            cmArr.Length.ShouldBeGreaterThanOrEqualTo(1);
            cmArr.ShouldContain(x => x.Id == id);
            #endregion
            //update
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            model.Name = "new name";
            res = await HttpClient.PutAsJsonAsync($"{AdminUri}/{id}", model);
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<Category>();

            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);
            cm.AdminComment.ShouldBe(model.AdminComment);

            //delete
            res = await HttpClient.DeleteAsync($"{AdminUri}/{id}");
            res.EnsureSuccessStatusCode();
            cm = await res.Content.ReadAsAsync<Category>();

            cm.Id.ShouldBe(id);
            cm.Name.ShouldBe(model.Name);
            cm.AdminComment.ShouldBe(model.AdminComment);

            //get deleted
            await Task.Delay(250);// wait for background tasks (by simulating network delay)
            res = await HttpClient.GetAsync($"{AdminUri}/{id}");
            res.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
