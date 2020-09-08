using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AnyService.SampleApp.Models;
using Newtonsoft.Json.Linq;
using Shouldly;
using AnyService.SampleApp.Identity;
using System.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E
{
    public class FilterFactoryTests : E2EFixture
    {
        public FilterFactoryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Use_ReservedQueries()
        {
            DbContext.Set<DependentModel>().RemoveRange(DbContext.Set<DependentModel>());
            var totalEntities = 6;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            for (int i = 0; i < totalEntities; i++)
            {
                var model = new
                {
                    Value = "init value_" + i + 1,
                    Public = i % 2 == 0,
                };
                var r = await HttpClient.PostAsJsonAsync("dependentmodel", model);

                r.EnsureSuccessStatusCode();
            }
            #region public
            var res = await HttpClient.GetAsync($"dependentmodel?query=__public");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities / 2);
            #endregion
            #region canRead
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canRead");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canRead");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion

            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canUpdate");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canUpdate");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"dependentmodel?query=__canDelete");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__canDelete");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
        }
    }
}
