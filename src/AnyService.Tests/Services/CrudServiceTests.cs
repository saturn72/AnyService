using System;
using System.Collections.Generic;
using System.Linq;
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

namespace AnyService.Tests.Services
{
    public class TestFileContainer : AuditableTestEntity, IFileContainer, ISoftDelete
    {
        public string Value { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
    public class AuditableTestEntity : IDomainEntity, IFullAudit, ISoftDelete
    {
        public string Id { get; set; }
        public bool Deleted { get; set; }
    }
    public class TestModel : IDomainEntity
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
    public class SoftDeleteEntity : IDomainEntity, ISoftDelete
    {
        public string Id { get; set; }
        public bool Deleted { get; set; }
    }
    public class OptionEntity : IDomainEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class AggregatedChild : IDomainEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class AggregateRootEntity : IDomainEntity
    {
        public string Id { get; set; }
        public OptionEntity Options { get; set; }
        public IEnumerable<AggregatedChild> Aggregated { get; set; }
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
            var sp = new Mock<IServiceProvider>();

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Create(new AuditableTestEntity());
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Create_BadRequestFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ReturnsAsync(null as AuditableTestEntity); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();


            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var model = new AuditableTestEntity();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData); eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEventData>()), Times.Never);
            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);
        }
        [Fact]
        public async Task Create_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var ex = new Exception();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ThrowsAsync(ex);
            var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var model = new AuditableTestEntity();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Error);
            eb.Verify(e => e.Publish(
                 It.Is<string>(s => s == ekr.Create),
                 It.Is<DomainEventData>(ed =>
                      ed.Data.GetPropertyValueByName<object>("incomingObject") == model &&
                      ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
                      ed.PerformedByUserId == wc.CurrentUserId)),
                 Times.Once);
            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);
        }
        [Fact]
        public async Task Create_Pass()
        {
            var model = new AuditableTestEntity();
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestEntity>())).ReturnsAsync(model); var v = new Mock<CrudValidatorBase<AuditableTestEntity>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestEntity>(), It.IsAny<ServiceResponse<AuditableTestEntity>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>();
            var am = new Mock<IAuditManager>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var fsm = new Mock<IFileStoreManager>();
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFileStoreManager))).Returns(fsm.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(model); mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestEntity>(e => e == model)), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                    It.IsAny<Type>(),
                    It.IsAny<string>(),
                    It.Is<string>(s => s == AuditRecordTypes.CREATE),
                    It.IsAny<object>())); eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
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
            var am = new Mock<IAuditManager>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestFileContainer),
                        EventKeys = ekr,
                    }
                },
            };
            var fsm = new Mock<IFileStoreManager>();
            fsm.Setup(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()))
            .ReturnsAsync
            (new[]{ new  FileStorageResponse
            {
                File  = file,
                Status = FileStoreState.Uploaded
            }});
            var logger = new Mock<ILogger<CrudService<TestFileContainer>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFileStoreManager))).Returns(fsm);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<TestFileContainer>(sp.Object, logger.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok); mp.Verify(a => a.PrepareForCreate(It.Is<TestFileContainer>(e => e == model)), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                It.IsAny<Type>(),
                It.IsAny<string>(),
                It.Is<string>(s => s == AuditRecordTypes.CREATE),
                It.IsAny<object>()));
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
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
                .Callback<string, ServiceResponse<AuditableTestEntity>>((str, sr) => sr.Result = ServiceResult.BadOrMissingData); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object); var id = "some-id";
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var model = new AuditableTestEntity(); var id = "123";
            var res = await cSrv.GetById(id); res.Result.ShouldBe(ServiceResult.Error); eb.Verify(e => e.Publish(
    It.Is<string>(s => s == ekr.Read),
    It.Is<DomainEventData>(ed =>
    ed.Data.GetPropertyValueByName<string>("incomingObject") == id &&
    ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
    ed.PerformedByUserId == wc.CurrentUserId)),
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object); var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => ed.Data == model && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task GetById_ReturnsDeletedByConfiguration_ShouldShowDeleted()
        {
            var sp = new Mock<IServiceProvider>();

            var data = new SoftDeleteEntity { Id = "a", Deleted = true };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ReturnsAsync(data);
            sp.Setup(s => s.GetService(typeof(IRepository<SoftDeleteEntity>))).Returns(repo.Object);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<SoftDeleteEntity>>()))
               .ReturnsAsync(true);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<SoftDeleteEntity>))).Returns(v.Object);

            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    ShowSoftDeleted = true,
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(SoftDeleteEntity),
                        EventKeys = new EventKeyRecord("create", "read", "update", "delete"),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();
            var cSrv = new CrudService<SoftDeleteEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetById("a");

            res.Result.ShouldBe(ServiceResult.Ok);
            res.Payload.ShouldBe(data);
        }
        [Fact]
        public async Task GetById_ReturnsDeletedByConfiguration_ShouldNotShowDeleted()
        {
            var sp = new Mock<IServiceProvider>();

            var data = new SoftDeleteEntity { Id = "a", Deleted = true };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ReturnsAsync(data);
            sp.Setup(s => s.GetService(typeof(IRepository<SoftDeleteEntity>))).Returns(repo.Object);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<string>(), It.IsAny<ServiceResponse<SoftDeleteEntity>>()))
               .ReturnsAsync(true);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<SoftDeleteEntity>))).Returns(v.Object);

            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(SoftDeleteEntity),
                        EventKeys = new EventKeyRecord("create", "read", "update", "delete"),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();
            var cSrv = new CrudService<SoftDeleteEntity>(sp.Object, logger.Object);

            var res = await cSrv.GetById("a");

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            repo.Verify(r => r.GetById(It.IsAny<string>()), Times.Once);
        }
        #region Aggregation

        #endregion
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFilterFactory))).Returns(ff.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object); var p = new Pagination<AuditableTestEntity>("id>0");
            var res = await cSrv.GetAll(p);
            res.Result.ShouldBe(ServiceResult.Ok);
            var data = res.Payload.GetPropertyValueByName<object>("Data");
            data.ShouldBeOfType<AuditableTestEntity[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
                    It.Is<string>(k => k == ekr.Read),
                    It.Is<DomainEventData>(ed => ed.Data == p && ed.PerformedByUserId == wc.CurrentUserId)),
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
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFilterFactory))).Returns(ff.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var model = new AuditableTestEntity(); var pagination = new Pagination<AuditableTestEntity>("id>0");
            var res = await cSrv.GetAll(pagination); res.Result.ShouldBe(ServiceResult.Error); eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Read),
                It.Is<DomainEventData>(ed =>
                ed.Data.GetPropertyValueByName<Pagination<AuditableTestEntity>>("incomingObject") == pagination &&
                ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
                ed.PerformedByUserId == wc.CurrentUserId)),
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
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<AuditableTestEntity>>(); var eb = new Mock<IEventBus>(); var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestEntity, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestEntity>(It.IsAny<string>())).ReturnsAsync(f); var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFilterFactory))).Returns(ff.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetAll(paginate);
            res.Result.ShouldBe(ServiceResult.Ok);
            paginate.Data.ShouldBe(dbRes);
            res.Payload.ShouldBe(paginate);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => ed.Data == paginate && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnsDeletedByConfiguration_DoShowDeleted_QueryRepository_With_Deleted()
        {
            var sp = new Mock<IServiceProvider>();
            var data = new[]
            {
                new SoftDeleteEntity { Id = "a", Deleted = true },
                new SoftDeleteEntity { Id = "b", },
                new SoftDeleteEntity { Id = "c", Deleted = true },
                new SoftDeleteEntity { Id = "d", },
            };
            var repo = new Mock<IRepository<SoftDeleteEntity>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<SoftDeleteEntity>>())).ReturnsAsync(data);
            sp.Setup(s => s.GetService(typeof(IRepository<SoftDeleteEntity>))).Returns(repo.Object);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<SoftDeleteEntity>>(), It.IsAny<ServiceResponse<Pagination<SoftDeleteEntity>>>()))
               .ReturnsAsync(true);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<SoftDeleteEntity>))).Returns(v.Object);

            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    ShowSoftDeleted = true,
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(SoftDeleteEntity),
                        EventKeys = new EventKeyRecord("create", "read", "update", "delete"),
                        PaginationSettings = new PaginationSettings { DefaultOffset = 100 },
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var ff = new Mock<IFilterFactory>();
            sp.Setup(s => s.GetService(typeof(IFilterFactory))).Returns(ff.Object);

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();
            var cSrv = new CrudService<SoftDeleteEntity>(sp.Object, logger.Object);
            var p = new Pagination<SoftDeleteEntity>(sde => sde.Id.HasValue());
            var res = await cSrv.GetAll(p);

            repo.Verify(r => r.GetAll(It.Is<Pagination<SoftDeleteEntity>>(p => data.Where(p.QueryFunc).Count() == data.Length)),
                    Times.Once);
        }

        [Fact]
        public async Task GetAll_ReturnsDeletedByConfiguration_DoNotShowDeleted_QueryRepository_Without_Deleted()
        {
            var sp = new Mock<IServiceProvider>();

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
            sp.Setup(s => s.GetService(typeof(IRepository<SoftDeleteEntity>))).Returns(repo.Object);

            var v = new Mock<CrudValidatorBase<SoftDeleteEntity>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<Pagination<SoftDeleteEntity>>(), It.IsAny<ServiceResponse<Pagination<SoftDeleteEntity>>>()))
               .ReturnsAsync(true);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<SoftDeleteEntity>))).Returns(v.Object);

            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(SoftDeleteEntity),
                        EventKeys = new EventKeyRecord("create", "read", "update", "delete"),
                        PaginationSettings = new PaginationSettings { DefaultOffset = 100 }
                    },
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var ff = new Mock<IFilterFactory>();
            sp.Setup(s => s.GetService(typeof(IFilterFactory))).Returns(ff.Object);

            var logger = new Mock<ILogger<CrudService<SoftDeleteEntity>>>();
            var cSrv = new CrudService<SoftDeleteEntity>(sp.Object, logger.Object);

            var p = new Pagination<SoftDeleteEntity>();
            var res = await cSrv.GetAll(p);

            res.Result.ShouldBe(ServiceResult.Ok);
            var actualData = data.Where(func);
            actualData.Count().ShouldBe(2);
            actualData.ShouldAllBe(x => data.Contains(x));
        }
        #endregion
        #region GetAggregated
        [Theory]
        [InlineData(null, new[] { "1" })]
        [InlineData("1", null)]
        [InlineData(null, null)]
        public async Task GetAggregated_NoAggregatedToFetch(string parentId, IEnumerable<string> aggToFetch)
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            }; var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);

            var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetAggregated(parentId, aggToFetch);
            var r = res.ShouldBeOfType<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>>();
            r.Result.ShouldBe(ServiceResult.BadOrMissingData);
            r.Payload.ShouldBeNull();
        }
        [Theory]
        [InlineData("not-exists")]
        [InlineData("not-exists,OptionEntity")]
        public async Task GetAggregated_NoAggregatedToFetch_IntersectWithAggregatedList(string toAggregate)
        {
            var aggToFetch = toAggregate.Split(",");
            var repo = new Mock<IRepository<AggregateRootEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);

            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetAggregated("pId", aggToFetch);
            var r = res.ShouldBeOfType<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>>();
            r.Result.ShouldBe(ServiceResult.BadOrMissingData);
            r.Payload.ShouldBeNull();
        }
        [Fact]
        public async Task GetAggregated_MissingRepositoryDefintionReturnsError()
        {
            var sp = new Mock<IServiceProvider>();

            var aggregateRootId = "p-id";
            var aggToFetch = new[] { "OptionEntity", "Aggregated" };
            var repo = new Mock<IRepository<AggregateRootEntity>>();
            var mapCollection = new[]
            {
                new EntityMapping
                {
                    Id = "em-a",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-a",
                },
                new EntityMapping
                {
                    Id = "em-b",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-b",
                },
                new EntityMapping
                {
                    Id = "em-b",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = "anther=parent",
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-c",
                },
                new EntityMapping
                {
                    Id = "em-c",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "Aggregated",
                    ChildId = "ch-d",
                },
                new EntityMapping
                {
                    Id = "em-d",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = "another-parent",
                    ChildEntityName = "Aggregated",
                    ChildId = "ch-e",
                },
            };
            var mapRepo = new Mock<IRepository<EntityMapping>>();
            mapRepo.Setup(mr => mr.Collection).ReturnsAsync(mapCollection.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);

            var oeCol = new[]
            {
                new  OptionEntity
                {
                    Id = "ch-a",
                    Name = "a"
                },
                new  OptionEntity
                {
                    Id = "ch-b",
                    Name = "b"
                },
                new  OptionEntity
                {
                    Id = "ch-c",
                    Name = "c"
                },
                new  OptionEntity
                {
                    Id = "ch-d",
                    Name = "d"
                },
            };
            var oeRepo = new Mock<IRepository<OptionEntity>>();
            oeRepo.Setup(oe => oe.Collection).ReturnsAsync(oeCol.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<OptionEntity>))).Returns(oeRepo.Object);

            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var arConfigRecord = new EntityConfigRecord
            {
                Type = typeof(AggregateRootEntity),
                Name = "AggregateRootEntity",
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[]
            {
                arConfigRecord,
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var eb = new Mock<IEventBus>();
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = arConfigRecord,
                },
            };
            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var am = new Mock<IAuditManager>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>))).Returns(repo.Object);

            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);

            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetAggregated(aggregateRootId, aggToFetch);
            res.Result.ShouldBe(ServiceResult.Error);
            res.Payload.ShouldBeNull();
        }

        [Fact]
        public async Task GetAggregated_ReturnAggregatedCollection()
        {
            var sp = new Mock<IServiceProvider>();

            var aggregateRootId = "p-id";
            var aggToFetch = new[] { "OptionEntity", "Aggregated" };
            var repo = new Mock<IRepository<AggregateRootEntity>>();
            var mapCollection = new[]
            {
                new EntityMapping
                {
                    Id = "em-a",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-a",
                },
                new EntityMapping
                {
                    Id = "em-b",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-b",
                },
                new EntityMapping
                {
                    Id = "em-b",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = "anther=parent",
                    ChildEntityName = "OptionEntity",
                    ChildId = "ch-c",
                },
                new EntityMapping
                {
                    Id = "em-c",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = aggregateRootId,
                    ChildEntityName = "Aggregated",
                    ChildId = "ch-d",
                },
                new EntityMapping
                {
                    Id = "em-d",
                    ParentEntityName = "AggregateRootEntity",
                    ParentId = "another-parent",
                    ChildEntityName = "Aggregated",
                    ChildId = "ch-e",
                },
            };
            var mapRepo = new Mock<IRepository<EntityMapping>>();
            mapRepo.Setup(mr => mr.Collection).ReturnsAsync(mapCollection.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);

            var oeCol = new[]
            {
                new  OptionEntity { Id = "ch-a", Name = "a" },
                new  OptionEntity { Id = "ch-b", Name = "b" },
                new  OptionEntity { Id = "ch-c", Name = "c" },
                new  OptionEntity { Id = "ch-d", Name = "d" },
            };
            var oeRepo = new Mock<IRepository<OptionEntity>>();
            oeRepo.Setup(oe => oe.Collection).ReturnsAsync(oeCol.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<OptionEntity>))).Returns(oeRepo.Object);

            var aggCol = new[]
            {
                new  AggregatedChild { Id = "ch-a", Name = "a" },
                new  AggregatedChild { Id = "ch-b", Name = "b" },
                new  AggregatedChild { Id = "ch-c", Name = "c" },
                new  AggregatedChild { Id = "ch-d", Name = "d" },
            };
            var aggRepo = new Mock<IRepository<AggregatedChild>>();
            aggRepo.Setup(oe => oe.Collection).ReturnsAsync(aggCol.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggRepo.Object);

            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var arConfigRecord = new EntityConfigRecord
            {
                Type = typeof(AggregateRootEntity),
                Name = "AggregateRootEntity",
                EventKeys = ekr,
                PaginationSettings = new PaginationSettings(),
            };
            var ecrs = new[]
            {
                arConfigRecord,
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var eb = new Mock<IEventBus>();
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = arConfigRecord,
                },
            };
            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var am = new Mock<IAuditManager>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>))).Returns(repo.Object);

            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);

            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var res = await cSrv.GetAggregated(aggregateRootId, aggToFetch);
            var r = res.ShouldBeOfType<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>>();
            r.Result.ShouldBe(ServiceResult.Ok);
            r.Payload.Count().ShouldBe(2);

            eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEventData>()), Times.Once);
        }
        #endregion
        #region GetAggregatedPage
        [Fact]
        public async Task GetAggregatedPage_EntityNotConfigured_ByEntityName()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new EntityConfigRecord[] { });

            var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var p = new Pagination<AggregatedChild>();
            var res = await cSrv.GetAggregatedPage(null, p, "not-exists");
            var r = res.ShouldBeOfType<ServiceResponse<Pagination<AggregatedChild>>>();
            r.Result.ShouldBe(ServiceResult.BadOrMissingData);
            r.Payload.ShouldBe(p);
        }
        [Fact]
        public async Task GetAggregatedPage_AggregatedEntityNotConfigured()
        {
            var repo = new Mock<IRepository<AuditableTestEntity>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object);
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new EntityConfigRecord[] { });

            var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var p = new Pagination<AggregatedChild>();
            var res = await cSrv.GetAggregatedPage(null, p);
            var r = res.ShouldBeOfType<ServiceResponse<Pagination<AggregatedChild>>>();
            r.Result.ShouldBe(ServiceResult.BadOrMissingData);
            r.Payload.ShouldBe(p);
        }
        [Fact]
        public async Task GetAggregatedPage_ParentNotExists()
        {
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };

            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus)));
            sp.Setup(s => s.GetService(typeof(IAuditManager)));

            var mapRepo = new Mock<IRepository<EntityMapping>>();
            mapRepo.Setup(mr => mr.Collection);
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);

            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var p = new Pagination<AggregatedChild>();
            var res = await cSrv.GetAggregatedPage<AggregatedChild>("not-exists", p);
            var r = res.ShouldBeOfType<ServiceResponse<Pagination<AggregatedChild>>>();
            r.Result.ShouldBe(ServiceResult.BadOrMissingData);
            r.Payload.ShouldBe(p);
        }
        [Fact]
        public async Task GetAggregatedPage_MissingRepositoryDefintionReturnsError()
        {
            var parentId = "p-id";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus)));
            sp.Setup(s => s.GetService(typeof(IAuditManager)));

            var maps = new[]
            {
                new EntityMapping{Id = "a", ParentEntityName = "AggregateRootEntity", ParentId = parentId, ChildEntityName = "aggregated", ChildId = "a"},
                new EntityMapping{Id = "b", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "e"},
                new EntityMapping{Id = "c", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "c"},
                new EntityMapping{Id = "d", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "d"},
            };
            var mapRepo = new Mock<IRepository<EntityMapping>>();
            mapRepo.Setup(mr => mr.Collection).ReturnsAsync(maps.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);

            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var p = new Pagination<AggregatedChild>();
            var res = await cSrv.GetAggregatedPage<AggregatedChild>(parentId, p);
            var r = res.ShouldBeOfType<ServiceResponse<Pagination<AggregatedChild>>>();
            r.Result.ShouldBe(ServiceResult.Error);
        }
        [Fact]
        public async Task GetAggregatedPage_GetPage()
        {
            var parentId = "p-id";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager)));

            var maps = new[]
            {
                new EntityMapping{Id = "a", ParentEntityName = "AggregateRootEntity", ParentId = parentId, ChildEntityName = "aggregated", ChildId = "a"},
                new EntityMapping{Id = "b", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "e"},
                new EntityMapping{Id = "c", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "c"},
                new EntityMapping{Id = "d", ParentEntityName = "AggregateRootEntity", ParentId = parentId,ChildEntityName = "aggregated", ChildId = "d"},
            };
            var mapRepo = new Mock<IRepository<EntityMapping>>();
            mapRepo.Setup(mr => mr.Collection).ReturnsAsync(maps.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);

            var page = new[]
            {
                new AggregatedChild{Id = "a"},
                new AggregatedChild{Id = "b"},
                new AggregatedChild{Id = "c"},
                new AggregatedChild{Id = "d"},
            };
            var aggRepo = new Mock<IRepository<AggregatedChild>>();
            aggRepo.Setup(ar => ar.GetAll(It.IsAny<Pagination<AggregatedChild>>())).ReturnsAsync(page);
            sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggRepo.Object);
            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var p = new Pagination<AggregatedChild>();
            var res = await cSrv.GetAggregatedPage(parentId, p);
            var r = res.ShouldBeOfType<ServiceResponse<Pagination<AggregatedChild>>>();
            r.Result.ShouldBe(ServiceResult.Ok);
            r.Payload.ShouldBe(p);
            p.Data.ShouldBe(page);

            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Read), It.Is<DomainEventData>(ded => ded.Data == p)), Times.Once);
        }
        #endregion
        #region UpdateMapping
        [Fact]
        public async Task UpdateMappings_NullRequest_ReturnsBadOrMissingData()
        {
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager)));
            var ecrs = new[]
          {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var res = await cSrv.UpdateMappings(null);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }

        [Fact]
        public async Task UpdateMappings_NamedChildTypeNotConfigured()
        {
            var parentId = "p-id";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    },
                }
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager)));
            var ecrs = new[]
          {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "Aggregated",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var req = new EntityMappingRequest
            {
                ParentId = parentId,
                ChildIdsToAdd = new[] { "a", "b", "c" },
                ChildIdsToRemove = new[] { "d", "e" },
                ChildEntityName = "not-exists",
            };
            var res = await cSrv.UpdateMappings(req);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task UpdateMappings_NotAllChildEntitiesExists_ReturnsBadResult()
        {
            var parentId = "p-id";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager)));
            var ecrs = new[]
          {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = "AggregateRootEntity",
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                 new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = typeof(AggregatedChild).Name,
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var addChData = new[]
            {
                new AggregatedChild{Id = "a"}
            };
            var aggChRepo = new Mock<IRepository<AggregatedChild>>();
            aggChRepo.Setup(ar => ar.Collection).ReturnsAsync(addChData.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggChRepo.Object);


            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var req = new EntityMappingRequest
            {
                ParentId = parentId,
                ChildIdsToAdd = new[] { "a", "b", "c" },
                ChildIdsToRemove = new[] { "d", "e" },
                ChildEntityName = typeof(AggregatedChild).Name,
            };
            var res = await cSrv.UpdateMappings(req);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task UpdateMappings_Maps()
        {
            var parentId = "p-id";
            var ekr = new EventKeyRecord("created", "read", null, "deleted");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AggregateRootEntity),
                        Name = "AggregateRootEntity",
                        EventKeys = ekr,
                        PaginationSettings = new PaginationSettings(),
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
            sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager)));
            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    Type = typeof(AggregateRootEntity),
                    Name = typeof(AggregateRootEntity).Name,
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                },
                new EntityConfigRecord
                {
                    Type = typeof(OptionEntity),
                    Name = "OptionEntity",
                },
                  new EntityConfigRecord
                {
                    Type = typeof(AggregatedChild),
                    Name = "AggregatedChild",
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

            var mapRepo = new Mock<IRepository<EntityMapping>>();
            var mapData = new[]
            {
                new EntityMapping
                {
                    ParentId = parentId,
                    ParentEntityName = typeof(AggregateRootEntity).Name,
                    ChildId = "d",
                    ChildEntityName = typeof(AggregatedChild).Name
                }
            };
            mapRepo.Setup(mr => mr.Collection).ReturnsAsync(mapData.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);
            var addChData = new[]
    {
                new AggregatedChild { Id = "a" },
                new AggregatedChild { Id = "b" },
                new AggregatedChild { Id = "c" },
            };
            var aggChRepo = new Mock<IRepository<AggregatedChild>>();
            aggChRepo.Setup(ar => ar.Collection).ReturnsAsync(addChData.AsQueryable());
            sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggChRepo.Object);

            var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
            var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
            var expIds = new[] { "a", "b", "c" };
            var req = new EntityMappingRequest
            {
                ParentId = parentId,
                ChildIdsToAdd = expIds,
                ChildIdsToRemove = new[] { "d", "e" },
                ChildEntityName = typeof(AggregatedChild).Name,
            };

            var res = await cSrv.UpdateMappings(req);
            res.Result.ShouldBe(ServiceResult.Ok);

            mapRepo.Verify(mr => mr.BulkDelete(It.Is<IEnumerable<EntityMapping>>(en => en.Count() == 1 && en.First().ChildId == "d"), It.IsAny<bool>()), Times.Once);
            Func<IEnumerable<EntityMapping>, bool> VerifyBulkInsertEntities =
                entities =>
                {
                    var cName = typeof(AggregatedChild).Name;
                    var pName = typeof(AggregateRootEntity).Name;
                    return entities.Count() == 3 &&
                    entities.All(e =>
                        e.ParentEntityName == pName &&
                        e.ParentId == parentId &&
                        e.ChildEntityName == cName &&
                        expIds.Contains(e.ChildId));
                };
            mapRepo.Verify(mr => mr.BulkInsert(It.Is<IEnumerable<EntityMapping>>(e => VerifyBulkInsertEntities(e)), It.IsAny<bool>()), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Delete), It.IsAny<DomainEventData>()), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
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
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            }; var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object); var res = await cSrv.Update(id, entity);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    },
                },
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object); var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Error); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Update),
                It.Is<DomainEventData>(ed =>
                     ed.Data.GetPropertyValueByName<string>("incomingObject") == id &&
                     ed.Data.GetPropertyValueByName<string>("exceptionId") == exId &&
                     ed.PerformedByUserId == wc.CurrentUserId)),
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.BadOrMissingData); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Never);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Error); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once); eb.Verify(e => e.Publish(
     It.Is<string>(s => s == ekr.Update),
     It.Is<DomainEventData>(ed =>
          ed.Data.GetPropertyValueByName<AuditableTestEntity>("incomingObject") == entity &&
          ed.Data.GetPropertyValueByName<string>("exceptionId") == exId &&
          ed.PerformedByUserId == wc.CurrentUserId)),
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Ok); v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestEntity>(ep => ep.Id == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestEntity>(e => e == dbModel), It.Is<AuditableTestEntity>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                       It.IsAny<Type>(),
                       It.IsAny<string>(),
                       It.Is<string>(s => s == AuditRecordTypes.UPDATE),
                       It.IsAny<object>()), Times.Once);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            }; var v = new Mock<CrudValidatorBase<TestFileContainer>>();
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestFileContainer),
                        EventKeys = ekr,
                    }
                },
            };
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<TestFileContainer>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<TestFileContainer>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<TestFileContainer>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IFileStoreManager))).Returns(fsm.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<TestFileContainer>(sp.Object, logger.Object);
            var res = await cSrv.Update(id, entity); res.Result.ShouldBe(ServiceResult.Ok); v.Verify(x => x.ValidateForUpdate(It.Is<TestFileContainer>(ep => ep.Id == id), It.IsAny<ServiceResponse<TestFileContainer>>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<TestFileContainer>(e => e == dbModel), It.Is<TestFileContainer>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                   It.IsAny<Type>(),
                   It.IsAny<string>(),
                   It.Is<string>(s => s == AuditRecordTypes.UPDATE),
                   It.IsAny<object>()), Times.Once);
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
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                    }
                },
            };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            }; var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var res = await cSrv.Delete(id); res.Result.ShouldBe(ServiceResult.Error); v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == id), It.IsAny<ServiceResponse<AuditableTestEntity>>()));
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Once); eb.Verify(e => e.Publish(
     It.Is<string>(s => s == ekr.Delete),
     It.Is<DomainEventData>(ed =>
          ed.Data.GetPropertyValueByName<string>("incomingObject") == id &&
          ed.Data.GetPropertyValueByName<string>("exceptionId") == exId &&
          ed.PerformedByUserId == wc.CurrentUserId)),
     Times.Once);
        }
        [Fact]
        public async Task Delete_NullOnRepositoryDelete()
        {
            var dbModel = new TestModel(); var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
     .ReturnsAsync(null as TestModel); var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestModel>>()))
                .ReturnsAsync(true); var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                        EventKeys = ekr,
                    }
                },
            };
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<TestModel>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<TestModel>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<TestModel>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc); var cSrv = new CrudService<TestModel>(sp.Object, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Delete_ThrowOnRepositoryDelete()
        {
            var dbModel = new TestModel(); var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
                .ThrowsAsync(ex); var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestModel>>()))
                .ReturnsAsync(true);
            var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                        EventKeys = ekr,
                    }
                },
            };
            var eb = new Mock<IEventBus>();
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<TestModel>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<TestModel>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<TestModel>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IIdGenerator))).Returns(gn.Object); var cSrv = new CrudService<TestModel>(sp.Object, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Error);
            eb.Verify(e => e.Publish(
               It.Is<string>(s => s == ekr.Delete),
               It.Is<DomainEventData>(ed =>
                    ed.Data.GetPropertyValueByName<string>("incomingObject") == id &&
                    ed.Data.GetPropertyValueByName<string>("exceptionId") == exId &&
                    ed.PerformedByUserId == wc.CurrentUserId)),
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Never);
            am.Verify(a => a.InsertAuditRecord(
                   It.IsAny<Type>(),
                   It.IsAny<string>(),
                   It.Is<string>(s => s == AuditRecordTypes.DELETE),
                   It.IsAny<object>()), Times.Never); eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Delete), It.IsAny<DomainEventData>()), Times.Never);
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
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(AuditableTestEntity),
                        EventKeys = ekr,
                    }
                },
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestEntity>>>();
            var am = new Mock<IAuditManager>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<AuditableTestEntity>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AuditableTestEntity>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<AuditableTestEntity>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            sp.Setup(s => s.GetService(typeof(IAuditManager))).Returns(am.Object); var cSrv = new CrudService<AuditableTestEntity>(sp.Object, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestEntity>(e => e == dbModel)), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                   It.IsAny<Type>(),
                   It.IsAny<string>(),
                   It.Is<string>(s => s == AuditRecordTypes.DELETE),
                   It.IsAny<object>())); eb.Verify(e => e.Publish(
     It.Is<string>(ek => ek == ekr.Delete),
     It.Is<DomainEventData>(
         ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        [Fact]
        public async Task Delete_Success()
        {
            var eb = new Mock<IEventBus>(); var dbModel = new TestModel(); var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel); repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
     .ReturnsAsync(dbModel); var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse<TestModel>>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                        EventKeys = ekr,
                    }
                },
            };
            var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>(); var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(typeof(AnyServiceConfig))).Returns(_config);
            sp.Setup(s => s.GetService(typeof(IRepository<TestModel>))).Returns(repo.Object);
            sp.Setup(s => s.GetService(typeof(CrudValidatorBase<TestModel>))).Returns(v.Object);
            sp.Setup(s => s.GetService(typeof(IModelPreparar<TestModel>))).Returns(mp.Object);
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object); var cSrv = new CrudService<TestModel>(sp.Object, logger.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEventData>(
                    ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        #endregion
    }
}
