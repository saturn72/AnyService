using Xunit;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AnyService.Core;

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
    }
    public class EfRepositoryTests
    {
        private readonly TestDbContext _dbContext;
        public EfRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: "test_ef_db")
                .Options;
            _dbContext = new TestDbContext(options);
        }
        [Fact]
        public async Task Insert()
        {
            var repo = new EfRepository<TestClass>(_dbContext);
            var entity = new TestClass
            {
                Value = "Some-value"
            };
            var inserted = await repo.Insert(entity);

            inserted.Id.ShouldNotBeEmpty();
            inserted.Value.ShouldBe(entity.Value);
        }

        [Fact]
        public void GetById()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void GetAll_WithoutFilter()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void GetAll_Filtered()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void Update()
        {
            throw new System.NotImplementedException();
        }

        [Fact]
        public void Delete()
        {
            throw new System.NotImplementedException();
        }
    }
}
