using Xunit;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using EFCore.BulkExtensions;

namespace AnyService.EntityFramework.Tests.Sql
{

    public class EfRepositorySqlServerTests
    {
        #region nnested classes
        public class SqlBulkTestClass : IDomainModelBase
        {
            public string Id { get; set; }
            public int Value { get; set; }
        }
        #endregion
        #region feilds
        private readonly SqlDbContext _dbContext;
        private readonly Mock<ILogger<EfRepository<SqlBulkTestClass>>> _logger;
        private readonly EfRepository<SqlBulkTestClass> _repository;
        private readonly DbContextOptions<SqlDbContext> _options;
        #endregion
        public EfRepositorySqlServerTests()
        {
            _options = new DbContextOptionsBuilder<SqlDbContext>()
                   .UseSqlServer(@"Data Source=.\SqlExpress;Initial Catalog=Test_DB;Integrated Security=True")
                   .Options;

            _dbContext = new SqlDbContext(_options);
            _logger = new Mock<ILogger<EfRepository<SqlBulkTestClass>>>();
            var bc = new BulkConfig
            {
                PreserveInsertOrder = true,
                BatchSize = 4000,
                PropertiesToExclude = new List<string> { nameof(IDomainModelBase.Id) },
            };
            _repository = new EfRepository<SqlBulkTestClass>(_dbContext, bc, _logger.Object);
        }

        [Fact]
        public async Task InsertBulk()
        {
            var total = 4;
            var entities = new List<SqlBulkTestClass>();
            for (int i = 0; i < total; i++)
                entities.Add(new SqlBulkTestClass { Value = i, });

            var inserted = await _repository.InsertBulk(entities);
            inserted.GetHashCode().ShouldNotBe(entities.GetHashCode());
            inserted.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
                inserted.ElementAt(i).Value.ShouldBe(i);
        }
    }
}
