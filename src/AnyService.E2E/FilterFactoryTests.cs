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
        private const string URI = "dependentmodel";
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
                var r = await HttpClient.PostAsJsonAsync(URI, model);

                r.EnsureSuccessStatusCode();
            }
            #region public
            var res = await HttpClient.GetAsync($"{URI}?query=__public");
            res.EnsureSuccessStatusCode();
            var dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(totalEntities / 2);
            #endregion
            #region canRead
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{URI}?query=__canRead");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"{URI}?query=__canRead");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(0);
            #endregion

            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{URI}?query=__canUpdate");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"{URI}?query=__canUpdate");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(0);
            #endregion
            #region canUpdate
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);
            res = await HttpClient.GetAsync($"{URI}?query=__canDelete");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(totalEntities);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"{URI}?query=__canDelete");
            res.EnsureSuccessStatusCode();
            dmArr = await res.Content.ReadAsAsync<DependentModel[]>();
            dmArr.Length.ShouldBe(0);
            #endregion
        }
    }
}
