using System;
using Xunit;
using AnyService.Core;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Shouldly;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;
using System.Collections.Generic;

namespace AnyService.LiteDb.Tests
{
    public class RepositoryTests
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

            var lr = new LiteDbRepository<TestDomainModel>(dbName);
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

        [Fact]
        public async Task GetAll()
        {
            var dbName = $"testdb-{GetCurrentMethodName()}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
            var entities = new[]{
                new TestDomainModel{Id = "1", Value = "1"},
                new TestDomainModel{Id = "2", Value = "2"},
                new TestDomainModel{Id = "3", Value = "2"},
            };

            using (var db = new LiteDatabase(dbName))
            {
                db.GetCollection<TestDomainModel>().Insert(entities);
            }
            var lr = new LiteDbRepository<TestDomainModel>(dbName);
            var all = await lr.GetAll(null);
            all.Count().ShouldBe(entities.Length);
            foreach (var e in entities)
                all.Any(x => x.Value == e.Value).ShouldBeTrue();

            var filter = new Dictionary<string, string> { { nameof(TestDomainModel.Value), "2" } };
            var allFiltered = await lr.GetAll(filter);
            allFiltered.Count().ShouldBe(2);
            foreach (var e in entities)
                allFiltered.All(x => x.Value == "2").ShouldBeTrue();
        }
    }
}
