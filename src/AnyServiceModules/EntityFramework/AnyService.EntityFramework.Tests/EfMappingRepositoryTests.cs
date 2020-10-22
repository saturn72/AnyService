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
using Microsoft.Data.Sqlite;
using System.Data.Common;
using AnyService.Mapping;
using AutoMapper.Extensions.ExpressionMapping;
using System.Linq.Expressions;

namespace AnyService.EntityFramework.Tests
{
    public class EfMappingRepositoryTests
    {
        private static EfMappingConfiguration config = new EfMappingConfiguration { MapperName = "ef-map-repo-tests" };

        #region nested classes
        public class TestNestedClass : IDomainEntity
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }
        public class TestEntity : IDomainEntity
        {
            public string Id { get; set; }
            public bool Flag { get; set; }
            public string Value { get; set; }
            public int Number { get; set; }
        }
        public class TestDbModel
        {
            public int Id { get; set; }
            public bool Flag { get; set; }
            public string Value { get; set; }
            public int Number { get; set; }
        }
        public class BulkTestEntity : IDomainEntity
        {
            public string Id { get; set; }
            public int Value { get; set; }
            public IEnumerable<TestNestedClass> TestNestedClasses
            {
                get; set;
            }
        }
        public class BulkTestDbModel
        {
            public int Id { get; set; }
            public int Value { get; set; }
            public IEnumerable<TestNestedClass> TestNestedClasses
            {
                get; set;
            }
        }
        #endregion
        private readonly TestDbContext _dbContext;
        private readonly Mock<ILogger<EfMappingRepository<TestEntity, TestDbModel>>> _logger;
        private readonly EfMappingRepository<TestEntity, TestDbModel> _repository;
        private static readonly DbContextOptions<TestDbContext> DbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(CreateDbConnection())
            .Options;

        private static DbConnection CreateDbConnection()
        {
            var c = new SqliteConnection("Filename=:memory:");
            c.Open();
            return c;
        }
        static EfMappingRepositoryTests()
        {
            var mf = new DefaultMapperFactory();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IMapperFactory))).Returns(mf);

            MappingExtensions.Configure(
                config.MapperName,
                cfg =>
                {
                    cfg.AddExpressionMapping();

                    cfg.CreateMap<TestEntity, TestDbModel>()
                    .ForMember(dest => dest.Id, mo => mo.MapFrom(src => int.Parse(src.Id)));
                    cfg.CreateMap<Pagination<TestEntity>, Pagination<TestDbModel>>();
                    cfg.CreateMap<TestDbModel, TestEntity>()
                    .ForMember(dest => dest.Id, mo => mo.MapFrom(src => src.Id.ToString()));
                    cfg.CreateMap<Pagination<TestDbModel>, Pagination<TestEntity>>();
                    cfg.CreateMap<BulkTestEntity, BulkTestDbModel>();
                    cfg.CreateMap<BulkTestDbModel, BulkTestEntity>();
                });

            MappingExtensions.Build(sp.Object);
        }
        public EfMappingRepositoryTests()
        {
            _logger = new Mock<ILogger<EfMappingRepository<TestEntity, TestDbModel>>>();
            _dbContext = new TestDbContext(DbOptions);

            _repository = new EfMappingRepository<TestEntity, TestDbModel>(config, _dbContext, _logger.Object);
        }

        [Fact]
        public async Task Insert()
        {
            var entity = new TestEntity
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
                .UseSqlite(CreateDbConnection())
                .Options;

            var ctx = new TestDbContext(options);

            var l = new Mock<ILogger<EfMappingRepository<BulkTestEntity, BulkTestDbModel>>>();
            var r = new EfMappingRepository<BulkTestEntity, BulkTestDbModel>(config, ctx, l.Object);
            var total = 4;
            var entities = new List<BulkTestEntity>();
            for (int i = 0; i < total; i++)
                entities.Add(new BulkTestEntity
                {
                    Value = i,
                    TestNestedClasses = new[] { new TestNestedClass { Value = i.ToString() } }
                });

            var inserted = await r.BulkInsert(entities);

            inserted.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
                inserted.ShouldContain(s => s.Value == i);

        }
        [Fact]
        public async Task GetById_returns_Null_On_NotExists()
        {
            var e = await _repository.GetById(int.MaxValue.ToString());
            e.ShouldBeNull();
        }
        [Fact]
        public async Task GetById()
        {
            _dbContext.Set<TestDbModel>().Add(new TestDbModel
            {
                Id = 1,
                Value = "value-abcd"
            });
            await _dbContext.SaveChangesAsync();
            var e = await _repository.GetById("1");
            e.Value.ShouldBe("value-abcd");
        }
        [Theory]
        [MemberData(nameof(GetAll_NullFilter_DATA))]
        public async Task GetAll_MissingFilter(Pagination<TestEntity> filter)
        {
            await Should.ThrowAsync<ArgumentNullException>(() => _repository.GetAll(filter));
        }
        public static IEnumerable<object[]> GetAll_NullFilter_DATA => new[]{
            new object[]{null },
            new object[]{new Pagination<TestEntity>() },
        };

        [Fact]
        public async Task GetAll_Filtered()
        {
            var tc = new List<TestDbModel>();
            _dbContext.Set<TestDbModel>().RemoveRange(_dbContext.Set<TestDbModel>());
            await _dbContext.SaveChangesAsync();
            var a = "a";
            for (int i = 0; i < 7; i++)
                tc.Add(new TestDbModel
                {
                    Value = i % 2 == 0 ? a : "b",
                });

            await _dbContext.Set<TestDbModel>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Expression<Func<TestEntity, bool>> q = x => x.Value == "a";
            var p = new Pagination<TestEntity>(q)
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
            }
        }
        [Fact]
        public async Task GetAll_Pagination()
        {
            var tc = new List<TestDbModel>();
            _dbContext.Set<TestDbModel>().RemoveRange(_dbContext.Set<TestDbModel>());
            await _dbContext.SaveChangesAsync();
            var count = 10;
            for (int i = 0; i < count; i++)
                tc.Add(new TestDbModel
                {
                    Number = i,
                });

            await _dbContext.Set<TestDbModel>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();

            Expression<Func<TestEntity, bool>> q = x => x.Id != "0";
            var p = new Pagination<TestEntity>(q)
            {
                OrderBy = nameof(TestEntity.Value),
                PageSize = 3,
            };
            var e = await _repository.GetAll(p);
            e.Count().ShouldBe(3);
            for (int i = 0; i < p.PageSize; i++)
                e.ShouldContain(s => s.Number == i);

            p.Data.ShouldBeNull();
            p.Total.ShouldBe(count);
        }
        [Fact]
        public async Task GetAll_PaginationWithOffset()
        {
            var tc = new List<TestDbModel>();
            _dbContext.Set<TestDbModel>().RemoveRange(_dbContext.Set<TestDbModel>());
            await _dbContext.SaveChangesAsync();
            var count = 10;
            for (int i = 0; i < count; i++)
                tc.Add(new TestDbModel
                {
                    Number = i,
                });

            await _dbContext.Set<TestDbModel>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();
            Expression<Func<TestEntity, bool>> q = x => x.Id.HasValue();

            var p = new Pagination<TestEntity>(q)
            {
                OrderBy = nameof(TestEntity.Number),
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
            var tc = new List<TestDbModel>();
            _dbContext.Set<TestDbModel>().RemoveRange(_dbContext.Set<TestDbModel>());
            await _dbContext.SaveChangesAsync();
            var a = "a";
            for (int i = 0; i < total; i++)
                tc.Add(new TestDbModel
                {
                    Flag = (i % 100) == 0,
                    Value = a,
                }); ;

            await _dbContext.Set<TestDbModel>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();

            foreach (var item in tc)
                _dbContext.Entry(item).State = EntityState.Detached;

            Func<TestEntity, bool> q = x => x.Value == a;
            var p = new Pagination<TestEntity>(x => q(x))
            {
                OrderBy = nameof(TestEntity.Flag),
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
            }
        }

        [Fact]
        public async Task Update_returnsNullOnEntityNotExists()
        {
            var updated = new TestEntity
            {
                Id = int.MaxValue.ToString(),
                Value = "new-value"
            };
            var res = await _repository.Update(updated);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task Update()
        {
            var orig = new TestDbModel
            {
                Value = "orig-value"
            };
            await _dbContext.Set<TestDbModel>().AddAsync(orig);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(orig).State = EntityState.Detached;

            var updated = new TestEntity
            {
                Id = orig.Id.ToString(),
                Value = "new-value"
            };
            var db = await _repository.Update(updated);

            var dbEntity = await _dbContext.Set<TestDbModel>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == orig.Id);
            dbEntity.Value.ShouldBe(updated.Value);
        }
        [Fact]
        public async Task Delete_ReturnsNullOnEntityNotExists()
        {
            var updated = new TestEntity
            {
                Value = "new-value"
            };
            var res = await _repository.Delete(updated);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task Delete()
        {
            var id = 999;
            var e = new TestDbModel
            {
                Id = id,
                Value = "value-abcd"
            };
            await _dbContext.Set<TestDbModel>().AddAsync(e);
            await _dbContext.SaveChangesAsync();
            _dbContext.Entry(e).State = EntityState.Detached;

            var toDelete = new TestEntity
            {
                Id = e.Id.ToString()
            };
            await _repository.Delete(toDelete);

            var entity = await _dbContext.Set<TestDbModel>().FindAsync(e.Id);
            entity.ShouldBeNull();
        }
    }
}
