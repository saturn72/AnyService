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
using AnyService.Services.Audit;

namespace AnyService.E2E
{
    public class AuditTests : E2EFixture
    {
        public AuditTests(ITestOutputHelper output) : base(output)
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
            await Task.Delay(1000);
            #region create
            var res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.CREATE}&entityNames=dependentmodel");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var jObj = JObject.Parse(content);
            var jArr = jObj["data"] as JArray;
            _output.WriteLine(jArr.ToString());
            jArr.Count.ShouldBe(totalEntities);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"dependentmodel?query=__created");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region public
            res = await HttpClient.GetAsync($"dependentmodel?query=__public");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(totalEntities / 2);
            #endregion
            #region updated
            var id1 = jArr.ElementAt(0).Value<string>("id");
            var id2 = jArr.ElementAt(1).Value<string>("id");

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);

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
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(1);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.PutAsJsonAsync($"dependentmodel/{id1}", updateModel);
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__updated");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
            #endregion
            #region delete
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson3);

            res = await HttpClient.DeleteAsync($"dependentmodel/{id1}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.DeleteAsync($"dependentmodel/{id2}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.GetAsync($"dependentmodel?query=__deleted");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(2);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);

            res = await HttpClient.DeleteAsync($"dependentmodel/{id1}");
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            res = await HttpClient.DeleteAsync($"dependentmodel/{id2}");
            res.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            content = await HttpClient.GetStringAsync($"dependentmodel?query=__deleted");
            jObj = JObject.Parse(content);
            jArr = jObj["data"] as JArray;
            jArr.Count.ShouldBe(0);
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
