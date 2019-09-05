using System;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using AnyService.Services;

namespace AnyService.LiteDb.Tests
{
    public class LiteDbModuleTests
    {
        public class TestDomainModel : IDomainModelBase
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCurrentMethodName()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }

        [Fact]
        public async Task Insert()
        {
            var dbName = $"testdb-{GetCurrentMethodName()}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";

            var lr = new AnyService.LiteDb.Repository<TestDomainModel>(dbName);
            var expValue = "my special value";
            var data = new TestDomainModel
            {
                Value = expValue
            };

            var dbRes = (await lr.Insert(data)) as TestDomainModel;
            dbRes.Id.ShouldNotBeNullOrEmpty();
            dbRes.Value.ShouldBe(expValue);
            dbRes.ShouldBe(data);
        }
    }
}
