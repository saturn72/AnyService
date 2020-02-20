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

namespace AnyService.Tests.Services
{
    public class TestFileContainer : TestModel, IFileContainer
    {
        public IEnumerable<FileModel> Files { get; set; }
    }
    public class TestModel : IDomainModelBase, IFullAudit
    {
        public string Id { get; set; }
        public string CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public bool Deleted { get; set; }
        public string DeletedOnUtc { get; set; }
        public string DeletedByUserId { get; set; }
        public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
    public class CrudServiceTests
    {
        readonly WorkContext _wc = new WorkContext
        {
            CurrentUserId = "some-user-id",
            CurrentEntityConfigRecord = new EntityConfigRecord()
        };
        #region Create
        [Fact]
        public async Task Create_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<TestModel, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null, null);
            var res = await cSrv.Create(new TestModel());
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task Create_ReturnsFailureFromRepository()
        {
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<TestModel>())).ReturnsAsync(null as TestModel);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null, null);
            var model = new TestModel();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<DomainEventData>()), Times.Never);
            ah.Verify(a => a.PrepareForCreate(It.Is<TestModel>(e => e == model), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
        }

        [Fact]
        public async Task Create_Pass()
        {
            var model = new TestModel();
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.Insert(It.IsAny<TestModel>())).ReturnsAsync(model);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
            var ekr = new EventKeyRecord("created", null, null, null);
            var fsm = new Mock<IFileStoreManager>();

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, fsm.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);

            ah.Verify(a => a.PrepareForCreate(It.Is<TestModel>(e => e == model), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Never);
        }
        [Fact]
        public async Task GetById_Returns_CallsFileStorage()
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
            var ekr = new EventKeyRecord("created", null, null, null);
            var fsm = new Mock<IFileStoreManager>();
            fsm.Setup(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()))
            .ReturnsAsync
            (new[]{ new  FileUploadResponse
            {
                File  = file,
                Status = UploadStatus.Uploaded
            }});
            var cSrv = new CrudService<TestFileContainer>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, fsm.Object);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);

            ah.Verify(a => a.PrepareForCreate(It.Is<TestFileContainer>(e => e == model), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
            fsm.Verify(f => f.Upload(It.IsAny<IEnumerable<FileModel>>()), Times.Once);
        }
        #endregion
        #region read by id
        [Fact]
        public async Task GetById_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null, null);
            var id = "some-id";
            var res = await cSrv.GetById(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBe(id);
        }
        [Fact]
        public async Task GetById_Returns_NullResponseFromDB()
        {
            var model = new TestModel();
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as TestModel);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null, null);
            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.NotFound);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task GetById_Returns_ResponseFromDB()
        {
            var model = new TestModel();
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(model);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, null);
            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => ed.Data == model && ed.PerformedByUserId == _wc.CurrentUserId)), Times.Once);
        }
        #endregion
        #region get all
        [Fact]
        public async Task GetAll_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<ServiceResponse>(sr => sr.Result = ServiceResult.BadOrMissingData);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null, null);
            var res = await cSrv.GetAll();
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task GetAll_Returns_NullResponseFromDB()
        {
            var model = new TestModel();
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync(null as IEnumerable<TestModel>);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, null);
            var res = await cSrv.GetAll();
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBeOfType<TestModel[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
              It.Is<string>(k => k == ekr.Read),
              It.Is<DomainEventData>(ed =>
                  (ed.Data as IEnumerable<object>).Count() == 0 && ed.PerformedByUserId == _wc.CurrentUserId)), Times.Once);
        }
        [Fact]
        public async Task GetAll_ReturnesResponseFromDB()
        {
            var model = new TestModel();
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetAll(It.IsAny<IDictionary<string, string>>()))
                .ReturnsAsync(new[] { model });

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForGet(It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);

            var ah = new Mock<AuditHelper>();

            var eb = new Mock<IDomainEventsBus>();

            var ekr = new EventKeyRecord(null, "read", null, null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, null);
            var res = await cSrv.GetAll();
            res.Result.ShouldBe(ServiceResult.Ok);
            (res.Data as IEnumerable<TestModel>).ShouldContain(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<DomainEventData>(ed => (ed.Data as IEnumerable<object>).Contains(model) && ed.PerformedByUserId == _wc.CurrentUserId)), Times.Once);
        }
        #endregion
        #region Update
        [Fact]
        public async Task Update_BadRequest_OnValidatorFailure()
        {
            var entity = new TestModel();
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<TestModel, ServiceResponse>((m, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null, null);
            var res = await cSrv.Update("123", entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            res.Data.ShouldBeNull();
        }
        [Fact]
        public async Task Update_GetByIdReturnsNull()
        {
            var id = "some-id";
            var entity = new TestModel();
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as TestModel);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, null, null, null, null);
            var res = await cSrv.Update(id, entity);
            res.Result.ShouldBe(ServiceResult.NotFound);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_Throws()
        {
            var id = "some-id";
            var entity = new TestModel();
            var dbModel = new TestModel
            {
                Id = id,
            };
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<TestModel>()))
                .ReturnsAsync(null as TestModel);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null, null);
            var res = await cSrv.Update(id, entity);
            var ekr = new EventKeyRecord(null, null, "update", null);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<TestModel>(e => e == entity), It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Never);
        }
        [Fact]
        public async Task Update_RepositoryUpdate_ReturnsUpdatedData()
        {
            var id = "some-id";
            var entity = new TestModel();
            var dbModel = new TestModel
            {
                Id = id,
            };
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);
            repo.Setup(r => r.Update(It.IsAny<TestModel>()))
                .ReturnsAsync(entity);

            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<TestModel>(e => e == entity), It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<DomainEventData>()), Times.Once);
        }
        [Fact]
        public async Task Update_DoesNotUpdateDeleted()
        {
            var id = "some-id";
            var entity = new TestModel();
            var dbModel = new TestModel
            {
                Id = id,
                Deleted = true
            };
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForUpdate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, null, null, null, null);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
        }
        #endregion
        #region Delete
        [Fact]
        public async Task Delete_Unauthorized_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<string, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.Unauthorized);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.Unauthorized);
            v.Verify(x => x.ValidateForDelete(It.Is<string>(s => s == epId), It.IsAny<ServiceResponse>()));
        }
        [Fact]
        public async Task Delete_NotFoundOnDatabase()
        {
            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(null as TestModel);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, null, null, null, null);
            var epId = "some-id";
            var res = await cSrv.Delete(epId);
            res.Result.ShouldBe(ServiceResult.NotFound);
        }
        [Fact]
        public async Task Delete_NullOnRepositoryUpdate()
        {
            var ah = new Mock<AuditHelper>();
            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<TestModel>()))
                .ReturnsAsync(null as TestModel);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, null, null, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            ah.Verify(a => a.PrepareForDelete(It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
        }

        [Fact]
        public async Task Delete_Success()
        {
            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IDomainEventsBus>();

            var dbModel = new TestModel();

            var repo = new Mock<IRepository<TestModel>>();
            repo.Setup(r => r.GetById(It.IsAny<string>()))
                .ReturnsAsync(dbModel);

            repo.Setup(r => r.Update(It.IsAny<TestModel>()))
                .ReturnsAsync(dbModel);

            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForDelete(It.IsAny<string>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(true);
            var ekr = new EventKeyRecord(null, null, null, "delete");
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            ah.Verify(a => a.PrepareForDelete(It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<DomainEventData>(
                    ed => ed.Data == dbModel && ed.PerformedByUserId == _wc.CurrentUserId)), Times.Once());
        }
        #endregion
    }
}
