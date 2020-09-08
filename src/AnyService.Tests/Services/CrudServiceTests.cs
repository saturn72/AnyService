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

namespace AnyService.Tests.Services
{
    public class TestFileContainer : AuditableTestModel, IFileContainer, ISoftDelete
    {
        public string Value { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
    public class AuditableTestModel : IDomainModelBase, IFullAudit, ISoftDelete
    {
        public string Id { get; set; }
       
        public bool Deleted { get; set; }
    }
    public class TestModel : IDomainModelBase
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class CrudServiceTests
    {
        AnyServiceConfig _config = new AnyServiceConfig();
        #region Create
        [Fact]
        public async Task Create_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestModel, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, null, v.Object, null, null, null, null, logger.Object, null, null, null, null);
            var res = await cSrv.Create(new AuditableTestModel());
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Create_BadRequestFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, null);
            var model = new AuditableTestModel();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEventData>()), Times.Never);
            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model)), Times.Once);
        }
        [Fact]
        public async Task Create_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ThrowsAsync(ex);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
            var model = new AuditableTestModel();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Error);

            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Create),
                It.Is<DomainEventData>(ed =>
                     ed.Data.GetPropertyValueByName<object>("incomingObject") == model &&
                     ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
                     ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);

            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model)), Times.Once);
        }

        [Fact]
        public async Task Create_Pass()
        {
            var model = new AuditableTestModel();
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ReturnsAsync(model);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var am = new Mock<IAuditManager>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var fsm = new Mock<IFileStoreManager>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, fsm.Object, logger.Object, null, null, null, am.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);

            mp.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model)), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                    It.IsAny<Type>(),
                    It.IsAny<string>(),
                    It.Is<string>(s => s == AuditRecordTypes.CREATE),
                    It.IsAny<object>())); eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Never);
        }
        [Fact]
        public async Task Create_CallsFileStorage()
        {
            var file = new FileModel { DisplayFileName = "this is fileName" };
            var model = new TestFileContainer
            {
                Files = new[] { file }
            };
            var repo = new Mock<IRepository<TestFileContainer>>();
            repo.Setup(r => r.Insert(It.IsAny<TestFileContainer>())).ReturnsAsync(model);

            var v = new Mock<CrudValidatorBase<TestFileContainer>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<TestFileContainer>>();
            var am = new Mock<IAuditManager>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
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

            var cSrv = new CrudService<TestFileContainer>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, fsm.Object, logger.Object, null, null, null, am.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);

            mp.Verify(a => a.PrepareForCreate(It.Is<TestFileContainer>(e => e == model)), Times.Once);
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
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, null, v.Object, null, null, null, null, logger.Object, null, null, null, null);
            var id = "some-id";
            var res = await cSrv.GetById(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBe(id);
        }
        [Fact]
        public async Task GetById_Returns_NullResponseFromDB()
        {
            var model = new AuditableTestModel();
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord("create", null, null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, null);
            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.NotFound);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task GetById_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>())).ThrowsAsync(ex);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
            var model = new AuditableTestModel();

            var id = "123";
            var res = await cSrv.GetById(id);

            res.Result.ShouldBe(ServiceResult.Error);

            eb.Verify(e => e.Publish(
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
            var model = new AuditableTestModel();
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(model);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, null);
            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => ed.Data == model && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }
        #endregion
        #region get all
        [Fact]
        public async Task GetAll_NullPaginationReturnesBadRequest()
        {
            var dbRes = new[]{
                new AuditableTestModel { Id = "1", },
                new AuditableTestModel { Id = "2", },
                new AuditableTestModel { Id = "3", },
                new AuditableTestModel { Id = "4", },
                };
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestModel>>()))
                .ReturnsAsync(dbRes);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                }
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, null, wc, eb.Object, null, logger.Object, null, null, null, null);
            var res = await cSrv.GetAll(null);
            
            res.Result.ShouldBe(ServiceResult.Ok);
            var p = res.Data.ShouldBeOfType<Pagination<AuditableTestModel>>();
            p.Data.ShouldBe(dbRes);
        }
        [Fact]
        public async Task GetAll_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, null, v.Object, null, null, null, null, logger.Object, null, null, null, null);
            var res = await cSrv.GetAll(null);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Theory]
        [MemberData(nameof(GetAll_EmptyQuery_DATA))]
        public async Task GetAll_EmptyQuery(Pagination<AuditableTestModel> pagination)
        {
            var dbRes = new[]{
                new AuditableTestModel { Id = "1", },
                new AuditableTestModel { Id = "2", },
                new AuditableTestModel { Id = "3", },
                new AuditableTestModel { Id = "4", },
                };
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestModel>>()))
                .ReturnsAsync(dbRes);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                }
            };

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, null, wc, eb.Object, null, logger.Object, null, null, null, null);
            var res = await cSrv.GetAll(pagination);
            res.Result.ShouldBe(ServiceResult.Ok);
            var p = res.Data.ShouldBeOfType<Pagination<AuditableTestModel>>();
            p.Data.ShouldBe(dbRes);
        }
        public static IEnumerable<object[]> GetAll_EmptyQuery_DATA => new[]{
            new object[]{ new Pagination<AuditableTestModel>() },
            new object[]{ new Pagination<AuditableTestModel>("") },
        };

        [Fact]
        public async Task GetAll_Returns_NullResponseFromDB()
        {
            var model = new AuditableTestModel();
            var dbRes = null as IEnumerable<AuditableTestModel>;
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestModel>>()))
                .ReturnsAsync(dbRes);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                }
            };

            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestModel, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestModel>(It.IsAny<string>())).ReturnsAsync(f);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, ff.Object, null, null);
            var p = new Pagination<AuditableTestModel>("id>0");
            var res = await cSrv.GetAll(p);
            res.Result.ShouldBe(ServiceResult.Ok);
            var data = res.Data.GetPropertyValueByName<object>("Data");
            data.ShouldBeOfType<AuditableTestModel[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
                    It.Is<string>(k => k == ekr.Read),
                    It.Is<DomainEventData>(ed => ed.Data == p && ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.GetAll(It.IsAny<Pagination<AuditableTestModel>>())).ThrowsAsync(ex);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();

            var exId = "exId" as object;
            var gn = new Mock<IIdGenerator>();
            gn.Setup(g => g.GetNext()).Returns(exId);
            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                }
            };

            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestModel, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestModel>(It.IsAny<string>())).ReturnsAsync(f);
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, ff.Object, null, null);
            var model = new AuditableTestModel();

            var pagination = new Pagination<AuditableTestModel>("id>0");
            var res = await cSrv.GetAll(pagination);

            res.Result.ShouldBe(ServiceResult.Error);

            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Read),
                It.Is<DomainEventData>(ed =>
                     ed.Data.GetPropertyValueByName<Pagination<AuditableTestModel>>("incomingObject") == pagination &&
                     ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
                     ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnesResponseFromDB()
        {
            var model = new AuditableTestModel();
            var paginate = new Pagination<AuditableTestModel>("id > 0");
            var dbRes = new[] { model };
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.Is<Pagination<AuditableTestModel>>(d => d == paginate)))
                .ReturnsAsync(dbRes);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();

            var eb = new Mock<IEventBus>();

            var ekr = new EventKeyRecord(null, "read", null, null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                    PaginationSettings = new PaginationSettings(),
                }
            };

            var ff = new Mock<IFilterFactory>();
            var f = new Func<object, Func<AuditableTestModel, bool>>(p => (x => x.Id != ""));
            ff.Setup(f => f.GetFilter<AuditableTestModel>(It.IsAny<string>())).ReturnsAsync(f);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, ff.Object, null, null);
            var res = await cSrv.GetAll(paginate);
            res.Result.ShouldBe(ServiceResult.Ok);
            paginate.Data.ShouldBe(dbRes);
            res.Data.ShouldBe(paginate);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => ed.Data == paginate && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once);
        }

        #endregion
        #region Update
        [Fact]
        public async Task Update_BadRequest_OnValidatorFailure()
        {
            var entity = new AuditableTestModel();
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestModel, ServiceResponse>((m, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, null, v.Object, null, null, null, null, logger.Object, null, null, null, null);
            var res = await cSrv.Update("123", entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task Update_RepositoryReturnsNull_OnGetDbEntity()
        {
            var id = "some-id";
            var entity = new AuditableTestModel();
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestModel);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, null, wc, null, null, logger.Object, null, null, null, null);
            var res = await cSrv.Update(id, entity);
            res.Result.ShouldBe(ServiceResult.NotFound);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_Throws_OnGetDbEntity()
        {
            var id = "some-id";
            var entity = new AuditableTestModel();
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ThrowsAsync(ex);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                },
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Error);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
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
            var entity = new AuditableTestModel();
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(null as AuditableTestModel);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == dbModel), It.Is<AuditableTestModel>(e => e == entity)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Never);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var entity = new AuditableTestModel();
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ThrowsAsync(ex);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Error);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == dbModel), It.Is<AuditableTestModel>(e => e == entity)), Times.Once);

            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Update),
                It.Is<DomainEventData>(ed =>
                     ed.Data.GetPropertyValueByName<AuditableTestModel>("incomingObject") == entity &&
                     ed.Data.GetPropertyValueByName<string>("exceptionId") == exId &&
                     ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_ReturnsUpdatedData()
        {
            var id = "some-id";
            var entity = new AuditableTestModel();
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(entity);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var am = new Mock<IAuditManager>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, am.Object);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            mp.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == dbModel), It.Is<AuditableTestModel>(e => e == entity)), Times.Once);
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
            var entity = new AuditableTestModel();
            var dbModel = new AuditableTestModel
            {
                Id = id,
                Deleted = true
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, null, wc, null, null, logger.Object, null, null, null, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
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
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var repo = new Mock<IRepository<TestFileContainer>>();
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
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var am = new Mock<IAuditManager>();

            var cSrv = new CrudService<TestFileContainer>(_config,
                repo.Object, v.Object,
                mp.Object, wc,
                eb.Object,
                fsm.Object, 
                logger.Object, null, null, null,
                am.Object);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestFileContainer>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
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
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<string, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.Unauthorized);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, null, v.Object, null, null, null, null, logger.Object, null, null, null, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == epId), It.IsAny<ServiceResponse>()));
        }
        [Fact]
        public async Task Delete_NotFoundOnDatabase()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, null, wc, null, null, logger.Object, null, null, null, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_NullOnRepositoryUpdate()
        {
            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var dbModel = new AuditableTestModel();

            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, null, null, logger.Object, null, null, null, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel)), Times.Once);
        }
        [Fact]
        public async Task delete_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ThrowsAsync(ex);

            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
            var res = await cSrv.Delete(id);

            res.Result.ShouldBe(ServiceResult.Error);

            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == id), It.IsAny<ServiceResponse>()));
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel)), Times.Once);

            eb.Verify(e => e.Publish(
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
            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
                .ReturnsAsync(null as TestModel);

            var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var cSrv = new CrudService<TestModel>(_config, repo.Object, v.Object, mp.Object, wc, null, null, logger.Object, null, null, null, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Delete_ThrowOnRepositoryDelete()
        {
            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
                .ThrowsAsync(ex);

            var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var eb = new Mock<IEventBus>();
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);

            var cSrv = new CrudService<TestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, gn.Object, null, null, null);
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
            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();

            var dbModel = new AuditableTestModel { Deleted = true };

            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var am = new Mock<IAuditManager>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, am.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel)), Times.Never);
            am.Verify(a => a.InsertAuditRecord(
                   It.IsAny<Type>(),
                   It.IsAny<string>(),
                   It.Is<string>(s => s == AuditRecordTypes.DELETE),
                   It.IsAny<object>()), Times.Never); 
            
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Delete), It.IsAny<DomainEventData>()), Times.Never);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_Success()
        {
            var mp = new Mock<IModelPreparar<AuditableTestModel>>();
            var eb = new Mock<IEventBus>();

            var dbModel = new AuditableTestModel();

            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<CrudValidatorBase<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var am = new Mock<IAuditManager>();
            var cSrv = new CrudService<AuditableTestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, am.Object);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            mp.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel)), Times.Once);
            am.Verify(a => a.InsertAuditRecord(
                   It.IsAny<Type>(),
                   It.IsAny<string>(),
                   It.Is<string>(s => s == AuditRecordTypes.DELETE),
                   It.IsAny<object>()));
            
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEventData>(
                    ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        [Fact]
        public async Task Delete_Success()
        {
            var eb = new Mock<IEventBus>();

            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<CrudValidatorBase<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var wc = new WorkContext
            {
                CurrentUserId = "some-user-id",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    EventKeys = ekr,
                }
            };
            var mp = new Mock<IModelPreparar<TestModel>>();
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var cSrv = new CrudService<TestModel>(_config, repo.Object, v.Object, mp.Object, wc, eb.Object, null, logger.Object, null, null, null, null);
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
