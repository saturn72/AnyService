using Xunit;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using AnyService.Services;
using System;

namespace AnyService.EntityFramework.Tests.Sql
{
    public class EfRepositorySqlServerTests
    {
        #region nested classes
        public class SqlBulkTestClass : IEntity
        {
            public string Id { get; set; }
            public int Value { get; set; }
            public string Description { get; set; }
        }
        public class SqlBulkTestClass2 : IEntity
        {
            public string Id { get; set; }
            public int Value { get; set; }
            public string Description { get; set; }
        }
        #endregion
        #region fields
        private readonly SqlDbContext _dbContext;
        private readonly EfRepositoryConfig _config;
        private readonly Mock<ILogger<EfRepository<SqlBulkTestClass>>> _logger;
        private readonly EfRepository<SqlBulkTestClass> _repository;
        private readonly Mock<ILogger<EfRepository<SqlBulkTestClass2>>> _logger2;
        private readonly EfRepository<SqlBulkTestClass2> _repository2;
        private readonly DbContextOptions<SqlDbContext> _options;
        #endregion
        public EfRepositorySqlServerTests()
        {
            _options = new DbContextOptionsBuilder<SqlDbContext>()
                   .UseSqlServer(@"Data Source=.\SqlExpress;Initial Catalog=AnyService_Test_DB;Integrated Security=True")
                   .Options;

            _dbContext = new SqlDbContext(_options);
            _dbContext.Database.ExecuteSqlRaw("DELETE FROM [Table2]");
            _config = new EfRepositoryConfig();
            _logger = new Mock<ILogger<EfRepository<SqlBulkTestClass>>>();

            _repository = new EfRepository<SqlBulkTestClass>(_dbContext, _config, _logger.Object);
            _logger2 = new Mock<ILogger<EfRepository<SqlBulkTestClass2>>>();
            _repository2 = new EfRepository<SqlBulkTestClass2>(_dbContext, _config, _logger2.Object);
        }


        [Fact]
        [Trait("category", "sql-server")]
        public async Task GetAllWithProjection()
        {
            var total = 100000;
            var iterations = 20;
            for (int j = 0; j < iterations; j++)
            {
                var entities = new List<SqlBulkTestClass2>();
                for (int i = 0; i < total/iterations; i++)
                    entities.Add(new SqlBulkTestClass2 { Id = Guid.NewGuid().ToString(), Value = i, Description = GenerateRandomstring() });
                _ = await _repository2.BulkInsert(entities);
            }

            var p = new Pagination<SqlBulkTestClass2>
            {
                QueryFunc = x => x.Id?.Length > 0,
                ProjectedFields = new[] { nameof(SqlBulkTestClass2.Value) },
                PageSize = int.MaxValue
            };
            var res = await _repository2.GetAll(p);
            res.Count().ShouldBe(total);
        }

        private static readonly Random r = new Random();
        private string GenerateRandomstring()
        {
            var len = r.Next(100, Text.Length);
            return Text.Substring(0, len);
        }

        [Fact]
        [Trait("category", "sql-server")]
        public async Task InsertBulk_DontTrackId()
        {
            var total = 40000;
            var entities = new List<SqlBulkTestClass>();
            for (int i = 0; i < total; i++)
                entities.Add(new SqlBulkTestClass { Id = Guid.NewGuid().ToString(), Value = i, });

            var inserted = await _repository.BulkInsert(entities);
            // inserted.GetHashCode().ShouldNotBe(entities.GetHashCode());
            inserted.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
                inserted.ElementAt(i).Value.ShouldBe(i);
        }
        [Fact]
        [Trait("category", "sql-server")]
        public async Task InsertBulk_TrackId()
        {
            var total = 40000;
            var entities = new List<SqlBulkTestClass>();
            for (int i = 0; i < total; i++)
                entities.Add(new SqlBulkTestClass { Value = i, });

            var inserted = await _repository.BulkInsert(entities, true);
            // inserted.GetHashCode().ShouldNotBe(entities.GetHashCode());
            inserted.Count().ShouldBe(total);
            for (int i = 0; i < total; i++)
            {
                var cur = inserted.ElementAt(i);
                cur.Id.ShouldNotBeNullOrEmpty();
                cur.Id.ShouldNotBeNullOrWhiteSpace();
                cur.Value.ShouldBe(i);
            }
        }
        private const string Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo.Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\nLorem ipsum dolor sit amet, consectetur adipiscing elit.Phasellus viverra velit et nisl porta sollicitudin.Sed sed purus vel magna ultricies pretium.Integer eget scelerisque justo. Donec sodales, lacus nec interdum tempor, risus metus varius lorem, et egestas est turpis sit amet sapien.Donec euismod odio vitae turpis cursus fringilla.Sed pellentesque sem et nisi sollicitudin, ac euismod leo dictum. Nulla tempor mi nec ultricies hendrerit.\nNunc velit ipsum, interdum sed eleifend ut, interdum eu est.Maecenas eu venenatis purus, in egestas eros. Pellentesque tempor tincidunt mollis. Donec volutpat mauris lacus, a porta sapien accumsan vitae.Vestibulum rutrum urna arcu. Phasellus in quam sagittis, mollis enim eu, pulvinar mi. Phasellus aliquet eleifend aliquam.\nFusce at lectus id metus consectetur mattis.Mauris vitae eros arcu. Morbi ut odio lectus. Vestibulum vehicula sem quis cursus vestibulum. Suspendisse in lorem aliquet, feugiat dui vel, sagittis nisi. Curabitur rutrum libero ut justo blandit, nec condimentum arcu laoreet. Maecenas rhoncus luctus augue eget gravida. Pellentesque nec leo vitae orci tincidunt sodales sed ac tortor.\nPhasellus nec lorem cursus sapien fringilla pharetra sit amet quis tortor.Maecenas quam nisl, mollis vitae tortor nec, sodales cursus eros.Cras faucibus eu metus vitae venenatis. Donec sed mauris et nulla volutpat efficitur ac ac nibh. Phasellus vitae maximus libero, at lacinia mauris.Sed ornare aliquam nisl. Phasellus libero lorem, cursus id maximus sed, venenatis ac metus.Phasellus condimentum leo sed tincidunt porttitor.\nVestibulum cursus ut dui non dapibus. Vivamus maximus purus tincidunt ipsum vehicula feugiat.Proin tristique augue vitae mi dictum, id ornare nisl feugiat. In quis vulputate arcu. Aenean a est tincidunt, ullamcorper lacus sit amet, ullamcorper lacus. Donec blandit accumsan lectus. Donec est sapien, efficitur ac augue sit amet, condimentum finibus mi.Mauris non lectus eu odio tincidunt condimentum.\n";
    }
}
