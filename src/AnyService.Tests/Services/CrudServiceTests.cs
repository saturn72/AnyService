using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core;
using Moq;
using AnyService.Audity;
using AnyService.Events;
using Shouldly;
using Xunit;
using AnyService.Services.FileStorage;
using AnyService.Services;
using Microsoft.Extensions.Logging;
using AnyService.Utilities;

namespace AnyService.Tests.Services
{
    public class TestFileContainer : AuditableTestModel, IFileContainer
    {
        public string Value { get; set; }
        public IEnumerable<FileModel> Files { get; set; }
    }
    public class AuditableTestModel : IDomainModelBase, IFullAudit
    {
        public string Id { get; set; }
        public string CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public bool Deleted { get; set; }
        public string DeletedOnUtc { get; set; }
        public string DeletedByUserId { get; set; }
        public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
    public class TestModel : IDomainModelBase
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class CrudServiceTests
    {
        #region Create
        [Fact]
        public async Task Create_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestModel, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(null, v.Object, null, null, null, null, logger.Object, null);
            var res = await cSrv.Create(new AuditableTestModel());
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Create_BadRequestFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
            var model = new AuditableTestModel();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEventData>()), Times.Never);
            ah.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task Create_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ThrowsAsync(ex);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
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

            ah.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }

        [Fact]
        public async Task Create_Pass()
        {
            var model = new AuditableTestModel();
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<AuditableTestModel>())).ReturnsAsync(model);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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

            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, fsm.Object, logger.Object, null);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);

            ah.Verify(a => a.PrepareForCreate(It.Is<AuditableTestModel>(e => e == model), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
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

            var v = new Mock<ICrudValidator<TestFileContainer>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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

            var cSrv = new CrudService<TestFileContainer>(repo.Object, v.Object, ah.Object, wc, eb.Object, fsm.Object, logger.Object, null);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);

            ah.Verify(a => a.PrepareForCreate(It.Is<TestFileContainer>(e => e == model), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Once);
        }
        #endregion
        #region read by id
        [Fact]
        public async Task GetById_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);

            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(null, v.Object, null, null, null, null, logger.Object, null);
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

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
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

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
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

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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

            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
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
        public async Task GetAll_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(null, v.Object, null, null, null, null, logger.Object, null);
            var res = await cSrv.GetAll(null);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task GetAll_Returns_NullResponseFromDB()
        {
            var model = new AuditableTestModel();
            var dbRes = null as IEnumerable<AuditableTestModel>;
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<AuditableTestModel>>()))
                .ReturnsAsync(dbRes);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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

            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
            var res = await cSrv.GetAll(null);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBeOfType<AuditableTestModel[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
                    It.Is<string>(k => k == ekr.Read),
                    It.Is<DomainEventData>(ed => ed.Data == dbRes && ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ErrorFromRepository()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<AuditableTestModel>>())).ThrowsAsync(ex);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
            var model = new AuditableTestModel();

            var filter = new Paginate<AuditableTestModel>();
            var res = await cSrv.GetAll(filter);

            res.Result.ShouldBe(ServiceResult.Error);

            eb.Verify(e => e.Publish(
                It.Is<string>(s => s == ekr.Read),
                It.Is<DomainEventData>(ed =>
                     ed.Data.GetPropertyValueByName<Paginate<AuditableTestModel>>("incomingObject") == filter &&
                     ed.Data.GetPropertyValueByName<object>("exceptionId") == exId &&
                     ed.PerformedByUserId == wc.CurrentUserId)),
                Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnesResponseFromDB()
        {
            var model = new AuditableTestModel();
            var paginate = new Paginate<AuditableTestModel>();
            var dbRes = new[] { model };
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetAll(It.Is<Paginate<AuditableTestModel>>(d => d == paginate)))
                .ReturnsAsync(dbRes);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();

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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<AuditableTestModel, ServiceResponse>((m, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(null, v.Object, null, null, null, null, logger.Object, null);
            var res = await cSrv.Update("123", entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task Update_RepositoryReturnsNull_OnGetDbEntity()
        {
            var id = "some-id";
            var entity = new AuditableTestModel();
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, null, wc, null, null, logger.Object, null);
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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            var ex = new Exception();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ThrowsAsync(ex);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(null as AuditableTestModel);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == entity), It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ThrowsAsync(ex);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Error);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == entity), It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);

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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<AuditableTestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(entity);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<AuditableTestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == entity), It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Once);
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
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, null, wc, null, null, logger.Object, null);
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

            var v = new Mock<ICrudValidator<TestFileContainer>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestFileContainer>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var repo = new Mock<IRepository<TestFileContainer>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<TestFileContainer>()))
                .ReturnsAsync(entity);
            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<TestFileContainer>(
                repo.Object, v.Object,
                ah.Object, wc,
                eb.Object,
                fsm.Object, logger.Object, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestFileContainer>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<AuditableTestModel>(e => e == entity), It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Once);
        }
        #endregion
        #region Delete
        [Fact]
        public async Task Delete_Unauthorized_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<string, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.Unauthorized);
            var logger = new Mock<ILogger<CrudService<AuditableTestModel>>>();
            var cSrv = new CrudService<AuditableTestModel>(null, v.Object, null, null, null, null, logger.Object, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.Unauthorized);
            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == epId), It.IsAny<ServiceResponse>()));
        }
        [Fact]
        public async Task Delete_NotFoundOnDatabase()
        {
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, null, wc, null, null, logger.Object, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.NotFound);
        }
        [Fact]
        public async Task Delete_IDeletableAudit_NullOnRepositoryUpdate()
        {
            var ah = new Mock<AuditHelper>();
            var dbModel = new AuditableTestModel();

            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(null as AuditableTestModel);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, null, null, logger.Object, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            ah.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task delete_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var dbModel = new AuditableTestModel
            {
                Id = id,
            };
            var v = new Mock<ICrudValidator<AuditableTestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            var ex = new Exception();
            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ThrowsAsync(ex);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, gn.Object);
            var res = await cSrv.Delete(id);

            res.Result.ShouldBe(ServiceResult.Error);

            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForDelete(It.Is<IDeletableAudit>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);

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

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
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
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, wc, null, null, logger.Object, null);
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

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
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
            var eb = new Mock<IDomainEventsBus>();
            var gn = new Mock<IIdGenerator>();
            var exId = "ex-id";
            gn.Setup(g => g.GetNext()).Returns(exId);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, wc, eb.Object, null, logger.Object, gn.Object);
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
        public async Task Delete_IDeletableAudit_Success()
        {
            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();

            var dbModel = new AuditableTestModel();

            var repo = new Mock<IRepository<AuditableTestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<AuditableTestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<ICrudValidator<AuditableTestModel>>();
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
            var cSrv = new CrudService<AuditableTestModel>(repo.Object, v.Object, ah.Object, wc, eb.Object, null, logger.Object, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            ah.Verify(a => a.PrepareForDelete(It.Is<AuditableTestModel>(e => e == dbModel), It.Is<string>(s => s == wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEventData>(
                    ed => ed.Data == dbModel && ed.PerformedByUserId == wc.CurrentUserId)), Times.Once());
        }
        [Fact]
        public async Task Delete_Success()
        {
            var eb = new Mock<IDomainEventsBus>();

            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Delete(It.IsAny<TestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<ICrudValidator<TestModel>>();
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
            var logger = new Mock<ILogger<CrudService<TestModel>>>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, wc, eb.Object, null, logger.Object, null);
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
