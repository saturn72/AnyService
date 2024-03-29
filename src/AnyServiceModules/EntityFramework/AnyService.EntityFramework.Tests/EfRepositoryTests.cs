using Xunit;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using System;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace AnyService.EntityFramework.Tests
{
    public class TestNestedClass : IEntity
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
    public class TestClass : IEntity
    {
        public string Id { get; set; }
        public bool Flag { get; set; }
        public string Value { get; set; }
        public IEnumerable<TestNestedClass> NestedClasses { get; set; }
        public int Number { get; set; }
    }
    public class BulkInsertTestClass : IEntity
    {
        public string Id { get; set; }
        public int Value { get; set; }
        public IEnumerable<TestNestedClass> TestNestedClasses
        {
            get; set;
        }
    }
    public class BulkUpdateTestClass : IEntity
    {
        public string Id { get; set; }
        public int Value { get; set; }
    }
    public class EfRepositoryTests
    {
        private readonly TestDbContext _dbContext;
        private readonly EfRepositoryConfig _config;
        private readonly Mock<ILogger<EfRepository<TestClass>>> _logger;
        private readonly EfRepository<TestClass> _repository;
        private static readonly DbContextOptions<TestDbContext> DbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "test.db")
            .Options;
        public EfRepositoryTests()
        {
            _dbContext = new TestDbContext(DbOptions);
            _config = new EfRepositoryConfig();
            _logger = new Mock<ILogger<EfRepository<TestClass>>>();
            _repository = new EfRepository<TestClass>(_dbContext, _config, null, _logger.Object);
        }
        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void BuildOrderByPropertyMethod_Equality(bool caseSensitive, bool expResult)
        {
            var f = new RepositoryForTest().Handler(caseSensitive);
            f("test", "TEST").ShouldBe(expResult);
        }
        public class RepositoryForTest : EfGenericRepository<TestClass, string>
        {
            public RepositoryForTest() : base(new TestDbContext(DbOptions), new EfRepositoryConfig(), null, null)
            {
            }
            public Func<string, string, bool> Handler(bool cs) => base.BuildOrderByPropertyMethod(cs);
        }
        [Fact]
        public async Task Insert()
        {
            var entity = new TestClass
            {
                Value = "Some-value"
            };
            var inserted = await _repository.Insert(entity);

            inserted.Id.ShouldNotBeEmpty();
            inserted.Value.ShouldBe(entity.Value);
        }
        [Fact]
        public async Task InsertBulk()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(@"Filename=test.db")
                .Options;
            var ctx = new TestDbContext(options);
            var sc = new ServiceCollection();
            sc.AddTransient<DbContext>(s => new TestDbContext(options));
            var sp = sc.BuildServiceProvider();

            var l = new Mock<ILogger<EfRepository<BulkInsertTestClass>>>();
            var r = new EfRepository<BulkInsertTestClass>(ctx, _config, sp, l.Object);
            var total = 4;
            var entities = new List<BulkInsertTestClass>();
            for (int i = 0; i < total; i++)
                entities.Add(new BulkInsertTestClass
                {
                    Id = Guid.NewGuid().ToString(),
                    Value = i,
                    TestNestedClasses = new[] { new TestNestedClass { Value = i.ToString() } }
                });

            var inserted = await r.BulkInsert(entities);

            inserted.GetHashCode().ShouldNotBe(entities.GetHashCode());
            inserted.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
                inserted.ElementAt(i).Value.ShouldBe(i);
        }
        [Fact]
        public async Task GetById_returns_Null_On_NotExists()
        {
            var e = await _repository.GetById("not-exists");
            e.ShouldBeNull();
        }
        [Fact]
        public async Task GetById()
        {
            var dbEntity = new TestClass
            {
                Id = "123",
                Value = "this-is-value"
            };

            await _dbContext.Set<TestClass>().AddAsync(dbEntity);
            await _dbContext.SaveChangesAsync();

            var e = await _repository.GetById(dbEntity.Id);
            e.Id.ShouldBe(dbEntity.Id);
            e.Value.ShouldBe(dbEntity.Value);
        }
        #region GetAll
        [Theory]
        [MemberData(nameof(GetAll_NullFilter_DATA))]
        public async Task GetAll_MissingFilter(Pagination<TestClass> filter)
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _repository.GetAll(filter));
        }
        public static IEnumerable<object[]> GetAll_NullFilter_DATA => new[]{
            new object[]{null },
            new object[]{new Pagination<TestClass>() },
        };

        [Fact]
        public async Task GetAll_Filtered()
        {
            var tc = new List<TestClass>();
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();
            var a = "a";
            for (int i = 0; i < 7; i++)
                tc.Add(new TestClass
                {
                    Value = i % 2 == 0 ? a : "b",
                    NestedClasses = new[]
                    {
                        new TestNestedClass
                        {
                            Value = "v1_" + i%2,
                        },
                        new TestNestedClass
                        {
                            Value = "v2_" + i%2,
                        },
                    },
                });

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Func<TestClass, bool> q = x => x.Value == "a";
            var p = new Pagination<TestClass>(x => q(x))
            {
                OrderBy = "Id",
                IncludeNested = true,
            };
            var e = await _repository.GetAll(p);
            e.Count().ShouldBe(4);
            p.Data.ShouldBeNull();
            p.Total.ShouldBe(4);
            for (int i = 0; i < e.Count(); i++)
            {
                e.Any(x => x.Id != null && x.Value == a).ShouldBeTrue();
                var c = e.ElementAt(i);
                c.NestedClasses.Count().ShouldBe(2);
                c.NestedClasses.ElementAt(0).Value.ShouldBe("v1_0");
                c.NestedClasses.ElementAt(1).Value.ShouldBe("v2_0");
            }
        }
        [Fact]
        public async Task GetAll_Pagination()
        {
            var tc = new List<TestClass>();
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();
            var count = 10;
            for (int i = 0; i < count; i++)
                tc.Add(new TestClass
                {
                    Number = i,
                });

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Func<TestClass, bool> q = x => x.Id.HasValue();

            var p = new Pagination<TestClass>(x => q(x))
            {
                OrderBy = nameof(TestClass.Value),
                PageSize = 3,
                SortOrder = PaginationSettings.Asc
            };
            var e = await _repository.GetAll(p);
            e.Count().ShouldBe(3);
            for (int i = 0; i < p.PageSize; i++)
                e.ElementAt(i).Number.ShouldBe(count - 1 - i);

            p.Data.ShouldBeNull();
            p.Total.ShouldBe(count);
        }
        [Fact]
        public async Task GetAll_PaginationWithOffset()
        {
            var tc = new List<TestClass>();
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();
            var count = 10;
            for (int i = 0; i < count; i++)
                tc.Add(new TestClass
                {
                    Number = i,
                });

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Func<TestClass, bool> q = x => x.Id.HasValue();

            var p = new Pagination<TestClass>(x => q(x))
            {
                OrderBy = nameof(TestClass.Number),
                PageSize = 3,
                Offset = 3,
                SortOrder = "desc",
            };
            var e = await _repository.GetAll(p);
            e.Count().ShouldBe(3);
            for (int i = 0; i < p.PageSize; i++)
                e.ElementAt(i).Number.ShouldBe(count - (p.Offset + i + 1));

            p.Data.ShouldBeNull();
            p.Total.ShouldBe(count);
        }
        [Fact]
        public async Task GetAll_Filter_PaginationWithoutNavProperties()
        {
            var total = 700;
            var tc = new List<TestClass>();
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();
            var a = "a";
            for (int i = 0; i < total; i++)
                tc.Add(new TestClass
                {
                    Id = "id-" + i,
                    Flag = (i % 100) == 0,
                    Value = a,
                    NestedClasses = new[]
                    {
                        new TestNestedClass
                        {
                            Value = "v1_" + i%2,
                        },
                    },
                }); ;

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Func<TestClass, bool> q = x => x.Value == a;
            var p = new Pagination<TestClass>(x => q(x))
            {
                OrderBy = nameof(TestClass.Flag),
                SortOrder = PaginationSettings.Desc,
                PageSize = total,
                IncludeNested = false,
            };
            var e = await _repository.GetAll(p);
            p.Data.ShouldBeNull();
            p.Total.ShouldBe(total);
            var f = e.Take(7);
            f.All(x => x.Flag).ShouldBeTrue();

            var t = e.Skip(7);
            t.All(x => x.Flag).ShouldBeFalse();
            for (int i = 0; i < e.Count(); i++)
            {
                e.Any(x => x.Id != null && x.Value == a).ShouldBeTrue();
                var c = e.ElementAt(i);
                c.NestedClasses.ShouldBeNull();
            }
        }
        [Fact]
        public async Task GetAll_WithProjection()
        {
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();
            var tc = new TestClass
            {
                Id = "proj-123",
                Flag = true,
                Value = "this is my value",
            };

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Func<TestClass, bool> q = x => x.Id == tc.Id;
            var p = new Pagination<TestClass>(x => q(x))
            {
                OrderBy = nameof(TestClass.Flag),
                SortOrder = PaginationSettings.Desc,
                IncludeNested = false,
                ProjectedFields = new[] { nameof(TestClass.Flag), nameof(TestClass.Value) }
            };
            var e = await _repository.GetAll(p);

            p.Data.ShouldBeNull();
            p.Total.ShouldBe(1);

            var c = e.ElementAt(0);
            c.Id.ShouldBe(default);
            c.Flag.ShouldBeTrue();
            c.Value.ShouldBe(tc.Value);
            c.NestedClasses.ShouldBe(default);
        }
        #endregion

        [Fact]
        public async Task Update_returnsNullOnEntityNotExists()
        {
            var updated = new TestClass
            {
                Id = "id-not-exists",
                Value = "new-value"
            };
            var res = await _repository.Update(updated);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task Update()
        {
            var orig = new TestClass
            {
                Value = "orig-value"
            };
            await _dbContext.Set<TestClass>().AddAsync(orig);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(orig).State = EntityState.Detached;

            var updated = new TestClass
            {
                Id = orig.Id,
                Value = "new-value"
            };
            var db = await _repository.Update(updated);

            var dbEntity = await _dbContext.Set<TestClass>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == orig.Id);
            dbEntity.Value.ShouldBe(updated.Value);
        }
        [Fact]
        public async Task UpdateBulk()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("update-db")
                .Options;
            var ctx = new TestDbContext(options);
            var sc = new ServiceCollection();
            var total = 4;
            var entities = new List<BulkUpdateTestClass>();
            for (int i = 0; i < total; i++)
                entities.Add(new BulkUpdateTestClass
                {
                    Id = Guid.NewGuid().ToString(),
                    Value = i
                });

            ctx.BulkUpdateTestClasses.AddRange(entities);
            ctx.SaveChanges();
            ctx.Dispose();

            var c = new TestDbContext(options);
            sc.AddTransient<DbContext>(p => new TestDbContext(options));
            var sp = sc.BuildServiceProvider();
            var l = new Mock<ILogger<EfRepository<BulkUpdateTestClass>>>();
            var r = new EfRepository<BulkUpdateTestClass>(c, _config, sp, l.Object);

            foreach (var e in entities)
                e.Value = 2 * e.Value;
            var updated = await r.BulkUpdate(entities);
            updated.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
                updated.ElementAt(i).Value.ShouldBe(2 * i);
        }
        [Fact]
        public async Task Delete_ReturnsNullOnEntityNotExists()
        {
            var updated = new TestClass
            {
                Id = "id-not-exists",
                Value = "new-value"
            };
            var res = await _repository.Delete(updated);
            res.ShouldBeNull();
        }

        [Fact]
        public async Task Delete()
        {
            var orig = new TestClass
            {
                Value = "orig-value"
            };
            await _dbContext.Set<TestClass>().AddAsync(orig);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(orig).State = EntityState.Detached;

            var toDelete = new TestClass
            {
                Id = orig.Id,
            };
            await _repository.Delete(toDelete);

            var dbEntity = await _dbContext.Set<TestClass>().FindAsync(toDelete.Id);
            dbEntity.ShouldBeNull();
        }
    }
}