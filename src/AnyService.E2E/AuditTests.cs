using System.Linq;
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
using System.Collections.Generic;

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
            var url = "Stock/";
            var ids = new List<string>();
            DbContext.Set<Stock>().RemoveRange(DbContext.Set<Stock>());
            var totalEntities = 6;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);
            for (int i = 0; i < totalEntities; i++)
            {
                var model = new
                {
                    Value = "init value_" + i + 1,
                    Public = i % 2 == 0,
                };
                var r = await HttpClient.PostAsJsonAsync(url, model);
                r.EnsureSuccessStatusCode();
                var c = await r.Content.ReadAsStringAsync();
                var j = JObject.Parse(c);
                ids.Add(j["id"].Value<string>());
            }
            await Task.Delay(1000);
            #region create
            var res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.CREATE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            var jArr = JArray.Parse(content);
            _output.WriteLine(jArr.ToString());
            jArr.Count.ShouldBe(totalEntities);

            //for later...

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.CREATE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion
            #region updated

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var updateModel = new Stock
            {
                Value = "updated-value",
                Public = false
            };
            res = await HttpClient.PutAsJsonAsync($"{url}{ids.ElementAt(0)}", updateModel);
            res.EnsureSuccessStatusCode();
            res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.UPDATE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(1);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.UPDATE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion
            #region delete
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            res = await HttpClient.DeleteAsync($"{url}{ids.ElementAt(0)}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.DeleteAsync($"{url}{ids.ElementAt(1)}");
            res.EnsureSuccessStatusCode();
            res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.DELETE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(2);

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson2);
            res = await HttpClient.GetAsync($"__audit?auditRecordTypes={AuditRecordTypes.DELETE}&entityNames=Stock");
            res.EnsureSuccessStatusCode();
            content = await res.Content.ReadAsStringAsync();
            jArr = JArray.Parse(content);
            jArr.Count.ShouldBe(0);
            #endregion
        }
    }
}
