using System;
using Xunit;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Shouldly;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;
using System.Collections.Generic;
using AnyService.Services;

namespace AnyService.LiteDb.Tests
{
    public class LiteDbRepositoryTests
    {
        public class TestDomainModel : IDomainObject
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCurrentMethodName()
        {
            var st = new StackTrace();
            return st.GetFrame(4).GetMethod().Name;
        }

        [Fact]
        public async Task Insert()
        {
            var name = GetCurrentMethodName();
            var dbName = $"testdb-{name}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";

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
        public async Task GetById()
        {
            var name = GetCurrentMethodName();
            var dbName = $"testdb-{name}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
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
            var expElem = entities.ElementAt(1);
            var e = await lr.GetById(expElem.Id);
            e.Value.ShouldBe(expElem.Value);
        }
        [Fact]
        public async Task Update()
        {
            var name = GetCurrentMethodName();
            var dbName = $"testdb-{name}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
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
            var toUpdate = new TestDomainModel { Id = "1", Value = "new Value" };
            var e = await lr.Update(toUpdate);
            e.ShouldBe(toUpdate);

            using (var db = new LiteDatabase(dbName))
            {
                var e1 = db.GetCollection<TestDomainModel>().FindById(toUpdate.Id);
                e1.Value.ShouldBe(toUpdate.Value);
            }
        }
        [Fact]
        public async Task GetAll()
        {
            var name = GetCurrentMethodName();
            var dbName = $"testdb-{name}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
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

            var q = $"{nameof(TestDomainModel.Value)} == 2";
            var p = new Pagination<TestDomainModel>(q);
            var allFiltered = await lr.GetAll(p);
            allFiltered.Count().ShouldBe(2);
            foreach (var e in entities)
                allFiltered.All(x => x.Value == "2").ShouldBeTrue();
        }
        [Fact]
        public async Task Delete()
        {
            var name = GetCurrentMethodName();
            var dbName = $"testdb-{name}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";

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
            var e = await lr.Delete(entities.ElementAt(0));
            e.Id.ShouldBe("1");

            using (var db = new LiteDatabase(dbName))
            {
                db.GetCollection<TestDomainModel>().Count().ShouldBe(entities.Length - 1);
            }
        }
    }
}
