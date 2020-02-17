using Xunit;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AnyService.Core;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace AnyService.EntityFramework.Tests
{
    public class TestClass : IDomainModelBase
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        { }
        public DbSet<TestClass> TestClasses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestClass>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
        }
    }
    public class EfRepositoryTests
    {
        private readonly TestDbContext _dbContext;
        private readonly EfRepository<TestClass> _repository;
        private readonly ITestOutputHelper _output;

        public EfRepositoryTests(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "test_ef_db")
                .Options;
            _dbContext = new TestDbContext(options);
            _repository = new EfRepository<TestClass>(_dbContext);
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

        [Fact]
        public async Task GetAll_WithoutFilter()
        {
            _dbContext.Set<TestClass>().RemoveRange(_dbContext.Set<TestClass>());
            await _dbContext.SaveChangesAsync();

            var valuePrefix = "value-";

            var tc = new List<TestClass>();
            for (int i = 0; i < 3; i++)
                tc.Add(new TestClass
                {
                    Value = valuePrefix + i.ToString()
                });

            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();

            var e = await _repository.GetAll();
            e.Count().ShouldBe(tc.Count);
            for (int i = 0; i < tc.Count; i++)
                e.Any(x => x.Id != null && x.Value == valuePrefix + i.ToString()).ShouldBeTrue();
        }

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
                    Value = i % 2 == 0 ? a : "b"
                });


            await _dbContext.Set<TestClass>().AddRangeAsync(tc);
            await _dbContext.SaveChangesAsync();

            var filter = new Dictionary<string, string> { { "value", "a" } };
            var e = await _repository.GetAll(filter);
            e.Count().ShouldBe(4);
            for (int i = 0; i < tc.Count; i++)
                e.Any(x => x.Id != null && x.Value == a).ShouldBeTrue();
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
            await _repository.Update(updated);

            var dbEntity = await _dbContext.Set<TestClass>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == orig.Id);
            dbEntity.Value.ShouldBe(updated.Value);
        }
    }
}
