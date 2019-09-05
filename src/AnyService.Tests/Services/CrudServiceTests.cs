using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Services;
using Moq;
using AnyService.Audity;
using AnyService.Events;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
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
        WorkContext _wc = new WorkContext { CurrentUserId = "some-user-id" };
        #region Create
        [Fact]
        public async Task Create_BadRequest_OnValidatorFailure()
        {
            var v = new Mock<ICrudValidator<TestModel>>();
            v.Setup(i => i.ValidateForCreate(It.IsAny<TestModel>(), It.IsAny<ServiceResponse>()))
                .ReturnsAsync(false)
                .Callback<TestModel, ServiceResponse>((ep, sr) => sr.Result = ServiceResult.BadOrMissingData);
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null);
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

            var eb = new Mock<IEventBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null);
            var model = new TestModel();
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            eb.Verify(e => e.Publish(It.IsAny<string>(), It.IsAny<EventData>()), Times.Never);
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
            var eb = new Mock<IEventBus>();

            var ekr = new EventKeyRecord("created", null, null, null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var res = await cSrv.Create(model);
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);

            ah.Verify(a => a.PrepareForCreate(It.Is<TestModel>(e => e == model), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<EventData>()), Times.Once);
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
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null);
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

            var eb = new Mock<IEventBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null);
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

            var eb = new Mock<IEventBus>();

            var ekr = new EventKeyRecord(null, "read", null, null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var res = await cSrv.GetById("123");
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBe(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<EventData>(ed =>
                    ed.Data.GetPropertyValueByName<object>("Data") == model
                    && ed.Data.GetPropertyValueByName<string>("CurrentUserId") == _wc.CurrentUserId)), Times.Once);
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
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null);
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

            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, "read", null, null);

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var res = await cSrv.GetAll();
            res.Result.ShouldBe(ServiceResult.Ok);
            res.Data.ShouldBeOfType<TestModel[]>().Length.ShouldBe(0);
            eb.Verify(e => e.Publish(
              It.Is<string>(k => k == ekr.Read),
              It.Is<EventData>(ed =>
                  ed.Data.GetPropertyValueByName<IEnumerable<TestModel>>("Data").Count() == 0
                  && ed.Data.GetPropertyValueByName<string>("CurrentUserId") == _wc.CurrentUserId)), Times.Once);
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

            var eb = new Mock<IEventBus>();

            var ekr = new EventKeyRecord(null, "read", null, null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var res = await cSrv.GetAll();
            res.Result.ShouldBe(ServiceResult.Ok);
            (res.Data as IEnumerable<TestModel>).ShouldContain(model);
            eb.Verify(e => e.Publish(
                It.Is<string>(k => k == ekr.Read),
                It.Is<EventData>(ed =>
                    ed.Data.GetPropertyValueByName<IEnumerable<TestModel>>("Data").Contains(model)
                    && ed.Data.GetPropertyValueByName<string>("CurrentUserId") == _wc.CurrentUserId)), Times.Once);
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
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null);
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

            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, null, null, null);
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
            var eb = new Mock<IEventBus>();
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, null);
            var res = await cSrv.Update(id, entity);
            var ekr = new EventKeyRecord(null, null, "update", null);

            res.Result.ShouldBe(ServiceResult.BadOrMissingData);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<TestModel>(e => e == entity), It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<EventData>()), Times.Never);
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
            var eb = new Mock<IEventBus>();
            var ekr = new EventKeyRecord(null, null, "update", null);
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var res = await cSrv.Update(id, entity);

            res.Result.ShouldBe(ServiceResult.Ok);

            v.Verify(x => x.ValidateForUpdate(It.Is<TestModel>(ep => ep.Id == id), It.IsAny<ServiceResponse>()));
            ah.Verify(a => a.PrepareForUpdate(It.Is<TestModel>(e => e == entity), It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Update), It.IsAny<EventData>()), Times.Once);
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
            var cSrv = new CrudService<TestModel>(null, v.Object, null, null, null, null);
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
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, null, null, null, null);
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
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, null, null);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
            ah.Verify(a => a.PrepareForDelete(It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
        }

        [Fact]
        public async Task Delete_Success()
        {
            var ah = new Mock<AuditHelper>();
            var eb = new Mock<IEventBus>();

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
            var cSrv = new CrudService<TestModel>(repo.Object, v.Object, ah.Object, _wc, eb.Object, ekr);
            var id = "some-id";
            var res = await cSrv.Delete(id);
            res.Result.ShouldBe(ServiceResult.Ok);
            ah.Verify(a => a.PrepareForDelete(It.Is<TestModel>(e => e == dbModel), It.Is<string>(s => s == _wc.CurrentUserId)), Times.Once);
            eb.Verify(e => e.Publish(
                It.Is<string>(ek => ek == ekr.Delete),
                It.Is<EventData>(ed => ed.GetPropertyValueByName<object>("Data").GetPropertyValueByName<object>("Data") == dbModel
                && ed.GetPropertyValueByName<object>("Data").GetPropertyValueByName<string>("CurrentUserId") == _wc.CurrentUserId)), Times.Once());
        }
        #endregion
    }
}
