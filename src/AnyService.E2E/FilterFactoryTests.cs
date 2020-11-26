using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shouldly;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E
{
    public class FilterFactoryTests : E2ETestBase
    {
        public FilterFactoryTests(E2EFixture fixture, ITestOutputHelper outputHelper) :
            base(fixture, outputHelper)
        {
        }

        [Fact]
        public async Task Use_ReservedQueries()
        {
            var uri = "api/mymodel";
            var totalEntities = 6;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            for (int i = 0; i < totalEntities; i++)
            {
                var model = new
                {
                    Value = "init value_" + i + 1,
                    Public = i % 2 == 0,
                };
                var r = await HttpClient.PostAsJsonAsync(uri, model);

                r.EnsureSuccessStatusCode();
            }
            #region public
            var res = await HttpClient.GetAsync($"{uri}?query=__public");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(totalEntities / 2);
            #endregion
            #region canRead
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{uri}?query=__canRead");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"{uri}?query=__canRead");
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion

            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{uri}?query=__canUpdate");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"{uri}?query=__canUpdate");
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion
            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{uri}?query=__canDelete");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"{uri}?query=__canDelete");
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion
        }
    }
}
