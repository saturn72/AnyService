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
    public class BulkTestClass : IEntity
    {
        public string Id { get; set; }
        public int Value { get; set; }
        public IEnumerable<TestNestedClass> TestNestedClasses
        {
            get; set;
        }
        public class EfRepositoryTests
        {
            private readonly TestDbContext _dbContext;
            private readonly Mock<ILogger<EfRepository<TestClass>>> _logger;
            private readonly EfRepository<TestClass> _repository;
            private static readonly DbContextOptions<TestDbContext> DbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "test.db")
                .Options;
            public EfRepositoryTests()
            {
                _dbContext = new TestDbContext(DbOptions);
                _logger = new Mock<ILogger<EfRepository<TestClass>>>();
                _repository = new EfRepository<TestClass>(_dbContext, _logger.Object);
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
            #region BulkInsert
            [Fact]
            public async Task BulkInsert()
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(@"Filename=test.db")
                    .Options;
                var ctx = new TestDbContext(options);

                var l = new Mock<ILogger<EfRepository<BulkTestClass>>>();
                var r = new EfRepository<BulkTestClass>(ctx, l.Object);
                var total = 4;
                var entities = new List<BulkTestClass>();
                for (int i = 0; i < total; i++)
                    entities.Add(new BulkTestClass
                    {
                        Id = Guid.NewGuid().ToString(),
                        Value = i,
                        TestNestedClasses = new[] { new TestNestedClass { Value = i.ToString() } }
                    });

                var inserted = await r.BulkInsert(entities);

                inserted.GetHashCode().ShouldBe(entities.GetHashCode());
                inserted.Count().ShouldBe(total);
                for (int i = 0; i < total; i++)
                    inserted.ElementAt(i).Value.ShouldBe(i);
            }
            #endregion
            #region Bulk Delete
            [Fact]
            public async Task BulkDelete()
            {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(@"Filename=test.db")
                    .Options;
                var ctx = new TestDbContext(options);

                var l = new Mock<ILogger<EfRepository<BulkTestClass>>>();
                var total = 4;
                var entities = new List<BulkTestClass>();
                for (int i = 0; i < total; i++)
                    entities.Add(new BulkTestClass
                    {
                        Id = Guid.NewGuid().ToString(),
                        Value = i,
                    });

                await ctx.AddRangeAsync(entities);

                var r = new EfRepository<BulkTestClass>(ctx, l.Object);
                var deleted = await r.BulkDelete(entities, true);

                deleted.GetHashCode().ShouldBe(entities.GetHashCode());
                deleted.Count().ShouldBe(total);
                for (int i = 0; i < total; i++)
                    deleted.ElementAt(i).Value.ShouldBe(i);
            }
            #endregion
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
}
