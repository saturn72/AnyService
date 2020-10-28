using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using AnyService.Audity;
using AnyService.Events;
using Shouldly;
using Xunit;
using AnyService.Services.FileStorage;
using AnyService.Services;
using Microsoft.Extensions.Logging;
using AnyService.Utilities;
using AnyService.Services.Preparars;
using AnyService.Services.Audit;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AnyService.Tests.Services
{
    public class TestFileContainer : AuditableTestEntity, IFileContainer, ISoftDelete
    {
        public string Value { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
    public class AuditableTestEntity : IEntity, IFullAudit, ISoftDelete
    {
        public string Id { get; set; }
        public bool Deleted { get; set; }
    }
    public class TestEntity : IEntity
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
    public class SoftDeleteEntity : IEntity, ISoftDelete
    {
        public string Id { get; set; }
        public bool Deleted { get; set; }
    }
    public class CrudServiceTests
    {
        AnyServiceConfig _config = new AnyServiceConfig();
        #region Create
        [Fact]
        public async Task Create_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestEntity, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
            };
            var ecrs = new[] { ecr };
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var res = await cSrv.Create(new AuditableTestEntity());
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Create_BadRequestFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ReturnsAsync(null as AuditableTestEntity);
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord("create", null, null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);
            var model = new AuditableTestEntity();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData); eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEvent>()), Times.Never);
            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);
        }
        [Fact]
        public async Task Create_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var ex = new Exception();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ThrowsAsync(ex); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var traceId = "traceId";

            var ekr = new EventKeyRecord("create", null, null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
                TraceId = traceId
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, null, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);
            var model = new AuditableTestEntity();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Error); eb.Verify(e => e.Publish(
     It.Is<string>(s => s == ekr.Create),
     It.Is<DomainEvent>(ed =>
          ed.Data.GetPropertyValueByName<object>("Data") == model &&
          ed.Data.GetPropertyValueByName<string>("TraceId") == traceId &&
          ed.PerformedByUserId == wc.CurrentUserId)),
     Times.Once); mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);
        }
        [Fact]
        public async Task Create_Pass()
        {
            var model = new AuditableTestEntity();
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ReturnsAsync(model); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();

            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", null, null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var fsm = new Mock<IFileStoreManager>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
              _config, repo.Object, v.Object,
              wc, mp.Object, eb.Object,
              fsm.Object, null, null,
              ecrs, logger.Object);

            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(model); mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);

            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEvent>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Never);
        }
        [Fact(Skip = "currently file upload not supportted")]
        public async Task Create_CallsFileStorage()
        {
            var file = new FileModel { DisplayFileName = "this is fileName" };
            var model = new TestFileContainer
            {
                Files = new[] { file }
            };
            var repo = new Mock<IRepository<TestFileContainer>>();
            repo.Setup(r => r.Insert(It.IsAny<TestFileContainer>())).ReturnsAsync(model); var v = new Mock<CrudValidatorBase<TestFileContainer>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse<TestFileContainer>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<TestFileContainer>>();

            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(TestFileContainer),
                    EventKeys = ekr,
                }
            };
            var fsm = new Mock<IFileStoreManager>();
            fsm.Setup(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()))
            .ReturnsAsync
            (new[]{ new  FileStorageResponse
            {
                File  = file,
                Status = FileStoreState.Uploaded
            }});
            var logger = new Mock<ILogger<CrudService<TestFileContainer>>>();

            var cSrv = new CrudService<TestFileContainer>(null, null, v.Object, wc, null, null, null, null, null, null, logger.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok); mp.Verify(a => a.PrepareForCreate(It.Is<TestFileContainer>(e => e == model)), Times.Once);

            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEvent>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Once);
        }
        #endregion
        #region read by id
        [Fact]
        public async Task GetById_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(false)
                .Callback<string, ServiceResponse<AuditableTestEntity>>((str, sr) => sr.Result = ServiceResult.BadOrMissingData); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.GetById(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Payload.ShouldBeNull();
        }
        [Fact]
        public async Task GetById_Returns_NullResponseFromDB()
        {
            var model = new AuditableTestEntity();
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestEntity); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.NotFound);
            res.Payload.ShouldBeNull();
        }
        [Fact]
        public async Task GetById_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ThrowsAsync(ex); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();


            var ekr = new EventKeyRecord(null, "read", null, null);
            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "t-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var model = new AuditableTestEntity(); var id = "123";
            var res = await cSrv.GetById(id); res.Result.ShouldBe(ServiceResult.Error);
            res.TraceId.ShouldBe(wc.TraceId);
            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Read),
                It.IsAny<DomainExceptionEvent>()),
                Times.Once);
        }
        [Fact]
        public async Task GetById_Returns_ResponseFromDB()
        {
            var model = new AuditableTestEntity();
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(model);
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEvent>(ed => ed.Data == model && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task GetById_ReturnsDeletedByConfiguration_ShouldShowDeleted()
        {
            var data = new SoftDeleteEntity { Id = "a", Deleted = true };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ReturnsAsync(data);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<SoftDeleteEntity>>()))
               .ReturnsAsync(true);

            var eb = new Mock<IEventBus>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(SoftDeleteEntity),
                EventKeys = new EventKeyRecord(null, "read", null, null),
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();

            var cSrv = new CrudService<SoftDeleteEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.GetById("a");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(data);
        }
        [Fact]
        public async Task GetById_ReturnsDeletedByConfiguration_ShouldNotShowDeleted()
        {

            var data = new SoftDeleteEntity { Id = "a", Deleted = true };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ReturnsAsync(data);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<SoftDeleteEntity>>()))
               .ReturnsAsync(true);

            var eb = new Mock<IEventBus>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(SoftDeleteEntity),
                EventKeys = new EventKeyRecord(null, "read", null, null),
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();

            var cSrv = new CrudService<SoftDeleteEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.GetById("a");
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            repo.Verify(r => r.GetById(It.IsAny<string>()), Times.Once);
        }
        #endregion
        #region get all
        [Fact]
        public async Task GetAll_NullPaginationReturnesBadRequest()
        {
            var dbRes = new[]{
                new AuditableTestEntity { Id = "1", },
                new AuditableTestEntity { Id = "2", },
                new AuditableTestEntity { Id = "3", },
                new AuditableTestEntity { Id = "4", },
                };
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestEntity>>()))
                .ReturnsAsync(dbRes); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<AuditableTestEntity>>(), It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(true);
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.GetAll(null); res.Result.ShouldBe(ServiceResult.Ok);
            var p = res.Payload.ShouldBeOfType<Pagination<AuditableTestEntity>>();
            p.Data.ShouldBe(dbRes);
        }
        [Fact]
        public async Task GetAll_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(
                It.IsAny<Pagination<AuditableTestEntity>>(),
                It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(false)
                .Callback<Pagination<AuditableTestEntity>, ServiceResponse<Pagination<AuditableTestEntity>>>((p, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };


            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var res = await cSrv.GetAll(null);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Payload.ShouldBeNull();
        }
        [Theory]
        [MemberData(nameof(GetAll_EmptyQuery_DATA))]
        public async Task GetAll_EmptyQuery(Pagination<AuditableTestEntity> pagination)
        {
            var dbRes = new[]{
                new AuditableTestEntity { Id = "1", },
                new AuditableTestEntity { Id = "2", },
                new AuditableTestEntity { Id = "3", },
                new AuditableTestEntity { Id = "4", },
                };
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestEntity>>()))
                .ReturnsAsync(dbRes); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<AuditableTestEntity>>(), It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(true);
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
               _config, repo.Object, v.Object,
               wc, null, eb.Object,
               null, null, null,
               ecrs, logger.Object);

            var res = await cSrv.GetAll(pagination);
            res.Result.ShouldBe(ServiceResult.Ok);
            var p = res.Payload.ShouldBeOfType<Pagination<AuditableTestEntity>>();
            p.Data.ShouldBe(dbRes);
        }
        public static IEnumerable<object[]> GetAll_EmptyQuery_DATA => new[]{
            new object[]{ new Pagination<AuditableTestEntity>() },
            new object[]{ new Pagination<AuditableTestEntity>("") },
        };
        [Fact]
        public async Task GetAll_Returns_NullResponseFromDB()
        {
            var model = new AuditableTestEntity();
            var dbRes = null as IEnumerable<AuditableTestEntity>;
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestEntity>>()))
                .ReturnsAsync(dbRes); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<AuditableTestEntity>>(), It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);
            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, ff.Object, null,
                ecrs, logger.Object);

            var p = new Pagination<AuditableTestEntity>("id>0");
            var res = await cSrv.GetAll(p);
            res.Result.ShouldBe(ServiceResult.Ok);
            var data = res.Payload.GetPropertyValueByName<object>("Data");
            data.ShouldBeOfType<AuditableTestEntity[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
                    It.Is<string>(k => k == ekr.Read),
                    It.Is<DomainEvent>(ed => ed.Data == p && ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var ex = new Exception();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestEntity>>())).ThrowsAsync(ex); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<AuditableTestEntity>>(), It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var ekr = new EventKeyRecord(null, "read", null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "trace-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };
            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f);

            var cSrv = new CrudService<AuditableTestEntity>(
              _config, repo.Object, v.Object,
              wc, null, eb.Object,
              null, ff.Object, null,
              ecrs, logger.Object);

            var model = new AuditableTestEntity(); var pagination = new Pagination<AuditableTestEntity>("id>0");
            var res = await cSrv.GetAll(pagination);
            res.Result.ShouldBe(ServiceResult.Error);
            res.TraceId.ShouldBe(wc.TraceId);
            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Read),
                It.IsAny<DomainExceptionEvent>()),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnesResponseFromDB()
        {
            var model = new AuditableTestEntity();
            var paginate = new Pagination<AuditableTestEntity>("id > 0");
            var dbRes = new[] { model };
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetAll(It.Is<Pagination<AuditableTestEntity>>(d => d == paginate)))
                .ReturnsAsync(dbRes); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<AuditableTestEntity>>(), It.IsAny<ServiceResponse<Pagination<AuditableTestEntity>>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>(); var ekr = new EventKeyRecord(null, "read", null, null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };
            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, ff.Object, null,
                ecrs, logger.Object);

            var res = await cSrv.GetAll(paginate);
            res.Result.ShouldBe(ServiceResult.Ok);
            paginate.Data.ShouldBe(dbRes);
            res.Payload.ShouldBe(paginate);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEvent>(ed => ed.Data == paginate && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnsDeletedByConfiguration_DoShowDeleted_QueryRepository_With_Deleted()
        {
            var data = new[]
            {
                new SoftDeleteEntity { Id = "a", Deleted = true },
                new SoftDeleteEntity { Id = "b", },
                new SoftDeleteEntity { Id = "c", Deleted = true },
                new SoftDeleteEntity { Id = "d", },
            };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<SoftDeleteEntity>>())).ReturnsAsync(data);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<SoftDeleteEntity>>(), It.IsAny<ServiceResponse<Pagination<SoftDeleteEntity>>>()))
               .ReturnsAsync(true);

            var eb = new Mock<IEventBus>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(SoftDeleteEntity),
                EventKeys = new EventKeyRecord("create", null, null, null),
                PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var ff = new Mock<IFilterFactory>();

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();
            var cSrv = new CrudService<SoftDeleteEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, ff.Object, null,
                ecrs, logger.Object);

            var p = new Pagination<SoftDeleteEntity>(sde => sde.Id.HasValue());
            var res = await cSrv.GetAll(p);
            repo.Verify(r => r.GetAll(It.Is<Pagination<SoftDeleteEntity>>(p => data.Where(p.QueryFunc).Count() == data.Length)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnsDeletedByConfiguration_DoNotShowDeleted_QueryRepository_Without_Deleted()
        {

            var data = new[]
           {
                new SoftDeleteEntity { Id = "a", Deleted = true },
                new SoftDeleteEntity { Id = "b", },
                new SoftDeleteEntity { Id = "c", Deleted = true },
                new SoftDeleteEntity { Id = "d", },
            };
            Func<SoftDeleteEntity, bool> func = null;
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<SoftDeleteEntity>>()))
                .ReturnsAsync(data)
                .Callback<Pagination<SoftDeleteEntity>>(p => func = p.QueryFunc);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<SoftDeleteEntity>>(), It.IsAny<ServiceResponse<Pagination<SoftDeleteEntity>>>()))
               .ReturnsAsync(true);

            var eb = new Mock<IEventBus>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(SoftDeleteEntity),
                EventKeys = new EventKeyRecord(null, "read", null, null),
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr,
            };

            var ff = new Mock<IFilterFactory>();

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();

            var cSrv = new CrudService<SoftDeleteEntity>(
                _config, repo.Object, v.Object,
                wc, null, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var p = new Pagination<SoftDeleteEntity>();
            var res = await cSrv.GetAll(p);
            res.Result.ShouldBe(ServiceResult.Ok);
            var actualData = data.Where(func);
            actualData.Count().ShouldBe(2);
            actualData.ShouldAllBe(x => data.Contains(x));
        }
        #endregion
        #region Update
        [Fact]
        public async Task Update_BadRequest_OnValidatorFailure()
        {
            var entity = new AuditableTestEntity();
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestEntity, ServiceResponse>((m, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var res = await cSrv.Update("123", entity); res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Payload.ShouldBeNull();
        }
        [Fact]
        public async Task Update_RepositoryReturnsNull_OnGetDbEntity()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestEntity); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, null,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Update(id, entity);
            res.Result.ShouldBe(ServiceResult.NotFound);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_Throws_OnGetDbEntity()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var dbModel = new AuditableTestEntity
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ThrowsAsync(ex); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "trace-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
               _config, repo.Object, v.Object,
               wc, mp.Object, eb.Object,
               null, null, null,
               ecrs, logger.Object);
            
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Error);
            res.TraceId.ShouldBe(wc.TraceId);
            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Update),
                It.IsAny<DomainExceptionEvent>()),
                Times.Once);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_ReturnsNull()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var dbModel = new AuditableTestEntity
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
                .ReturnsAsync(null as AuditableTestEntity); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, null,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.BadOrMissingData); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEvent>()), Times.Never);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var dbModel = new AuditableTestEntity
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
                .ThrowsAsync(ex); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "trace-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Error); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            res.TraceId.ShouldBe(wc.TraceId);

            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Update),
                It.IsAny<DomainExceptionEvent>()),
                Times.Once);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_ReturnsUpdatedData()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var dbModel = new AuditableTestEntity
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
                .ReturnsAsync(entity); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Ok); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEvent>()), Times.Once);
        }
        [Fact]
        public async Task Update_DoesNotUpdateDeleted()
        {
            var id = "some-id";
            var entity = new AuditableTestEntity();
            var dbModel = new AuditableTestEntity
            {
                Id = id,
                Deleted = true
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, null, null,
                null, null, null,
                ecrs, logger.Object);
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.BadOrMissingData); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
        }
        [Fact]
        public async Task Update_CallsFileStorage()
        {
            var id = "some-id";
            var file = new FileModel { DisplayFileName = "this is fileName" };
            var dbModel = new TestFileContainer
            {
                Id = id,
                Value = "some-data",
            };
            var entity = new TestFileContainer
            {
                Value = "some-new-data",
                Files = new[] { file }
            };
            var v = new Mock<CrudValidatorBase<TestFileContainer>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse<TestFileContainer>>()))
                .ReturnsAsync(true); var repo = new Mock<IRepository<TestFileContainer>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<TestFileContainer>()))
                .ReturnsAsync(entity);
            var mp = new Mock<IModelPreparar<TestFileContainer>>();
            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<TestFileContainer>>>();
            var fsm = new Mock<IFileStoreManager>();
            fsm.Setup(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()))
            .ReturnsAsync
            (new[]{ new  FileStorageResponse
            {
                File  = file,
                Status = FileStoreState.Uploaded
            }});
            var ekr = new EventKeyRecord(null, null, "update", null);

            var ecr = new EntityConfigRecord
            {
                Type = typeof(TestFileContainer),
                EventKeys = ekr,
                ShowSoftDelete = true,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<TestFileContainer>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                fsm.Object, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Ok);
            v.Verify(x => x.ValidateForUpdate(It.Is<TestFileContainer>(ep => ep.Id == id), It.IsAny<ServiceResponse<TestFileContainer>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<TestFileContainer>(e => e == dbModel), It.Is<TestFileContainer>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEvent>()), Times.Once);
        }
        #endregion
        #region Delete
        [Fact]
        public async Task Delete_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(false)
                .Callback<string, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.Unauthorized);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == epId), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
        }
        [Fact]
        public async Task Delete_NotFoundOnDatabase()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestEntity); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_NullOnRepositoryUpdate()
        {
            var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var dbModel = new AuditableTestEntity(); var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
     .ReturnsAsync(null as AuditableTestEntity); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, null,
                null, null, null,
                ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Once);
        }
        [Fact]
        public async Task Delete_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var dbModel = new AuditableTestEntity
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
                .ThrowsAsync(ex); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "trace-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };


            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();

            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Error);
            res.TraceId.ShouldBe(wc.TraceId);
            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Once);
            eb.Verify(e => e.Publish(
                 It.Is<string>(s => s == ekr.Delete),
                 It.IsAny<DomainExceptionEvent>()),
                 Times.Once);
        }
        [Fact]
        public async Task Delete_NullOnRepositoryDelete()
        {
            var dbModel = new TestEntity(); var repo = new Mock<IRepository<TestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Delete(It.IsAny<TestEntity>()))
     .ReturnsAsync(null as TestEntity); var v = new Mock<CrudValidatorBase<TestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<TestEntity>>();
            var logger = new Mock<ILogger<CrudService<TestEntity>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(TestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var cSrv = new CrudService<TestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Delete_ThrowOnRepositoryDelete()
        {
            var dbModel = new TestEntity(); var repo = new Mock<IRepository<TestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Delete(It.IsAny<TestEntity>()))
                .ThrowsAsync(ex); var v = new Mock<CrudValidatorBase<TestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestEntity>>()))
                .ReturnsAsync(true);
            var mp = new Mock<IModelPreparar<TestEntity>>();
            var logger = new Mock<ILogger<CrudService<TestEntity>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(TestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                TraceId = "trace-id",
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var eb = new Mock<IEventBus>();

            var cSrv = new CrudService<TestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);

            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Error);
            res.TraceId.ShouldBe(wc.TraceId);
            eb.Verify(e => e.Publish(
               It.Is<string>(s => s == ekr.Delete),
               It.IsAny<DomainExceptionEvent>()),
               Times.Once);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_SoftDelete_AlreadyDeleted_ReturnsBadRequest()
        {
            var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>(); var dbModel = new AuditableTestEntity { Deleted = true }; var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
     .ReturnsAsync(dbModel); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();


            var cSrv = new CrudService<AuditableTestEntity>(null, null, v.Object, wc, null, null, null, null, null, ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Never);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_Success()
        {
            var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>(); var dbModel = new AuditableTestEntity(); var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Update(It.IsAny<AuditableTestEntity>()))
     .ReturnsAsync(dbModel); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(AuditableTestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();


            var cSrv = new CrudService<AuditableTestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Once);

            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEvent>(ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        [Fact]
        public async Task Delete_Success()
        {
            var eb = new Mock<IEventBus>(); var dbModel = new TestEntity(); var repo = new Mock<IRepository<TestEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Delete(It.IsAny<TestEntity>()))
     .ReturnsAsync(dbModel); var v = new Mock<CrudValidatorBase<TestEntity>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestEntity>>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");

            var ecr = new EntityConfigRecord
            {
                Type = typeof(TestEntity),
                EventKeys = ekr,
            };
            var ecrs = new[] { ecr };

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = ecr
            };
            var mp = new Mock<IModelPreparar<TestEntity>>();
            var logger = new Mock<ILogger<CrudService<TestEntity>>>();

            var cSrv = new CrudService<TestEntity>(
                _config, repo.Object, v.Object,
                wc, mp.Object, eb.Object,
                null, null, null,
                ecrs, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEvent>(
                    ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        #endregion
    }
}
