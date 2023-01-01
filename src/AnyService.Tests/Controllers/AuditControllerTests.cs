using System.Reflection;
using AnyService.Audity;
using AnyService.Controllers;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Tests.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace AnyService.Tests.Controllers
{
    public class AuditControllerTests : MappingTest
    {

        IEnumerable<EntityConfigRecord> _ecrs = new[]
        {
                new EntityConfigRecord{
                    Type = typeof(AuditRecord),
                    EndpointSettings = new EndpointSettings
                    {
                        PropertiesProjectionMap = new Dictionary<string, string>
                        {
                            { nameof(AuditRecord.EntityId), nameof(AuditRecordModel.EntityId) },
                        }
                    }
                }
        };
        [Fact]
        public void ValidateRoute()
        {
            var type = typeof(AuditController);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("__audit");
        }
        [Theory]
        [InlineData(nameof(AuditController.GetAll), "GET", null)]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(AuditController);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }
        public class MyClass : IEntity
        {
            public string Id { get; set; }
        }
        [Theory]
        [InlineData("ttt")]
        [InlineData("ttt, " + AuditRecordTypes.CREATE)]
        public async Task GetAll_UnknownAuditRecordType_ReturnsBadRequest(string auditRecordTypes)
        {
            var l = new Mock<ILogger<AuditController>>();
            var wc = new WorkContext { CurrentClientId = "1232" };
            var ctrl = new AuditController(null, null, wc, null, _ecrs, l.Object);

            var res = await ctrl.GetAll(auditRecordTypes: auditRecordTypes);
            res.ShouldBeOfType<BadRequestResult>();
        }
        [Theory]
        [InlineData("ttt", null)]
        [InlineData(null, "ttt")]
        [InlineData("ttt", "rrr")]
        public async Task GetAll_InvalidFrom_Todates_ReturnsBadRequest(string fromUtc, string toUtc)
        {
            var l = new Mock<ILogger<AuditController>>();
            var ctrl = new AuditController(null, null, null, null, _ecrs, l.Object);

            var res = await ctrl.GetAll(fromUtc: fromUtc, toUtc: toUtc);
            res.ShouldBeOfType<BadRequestResult>();
        }
        public class MyAuditableClass : IEntity, ICreatableAudit
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task GetAll_ReturnsPage()
        {
            var page = new AuditPagination
            {
                Data = new[]
                {
                    new AuditRecord{Id = "a"},
                    new AuditRecord{Id = "b"},
                    new AuditRecord{Id = "c"},
                    new AuditRecord{Id = "d"},
                }
            };
            var aSrv = new Mock<IAuditManager>();
            aSrv.Setup(c => c.GetAll(It.IsAny<AuditPagination>()))
                .ReturnsAsync(new ServiceResponse<AuditPagination> { Payload = page, Result = ServiceResult.Ok });

            var l = new Mock<ILogger<AuditController>>();
            var rm = new Mock<IServiceResponseMapper>();
            ServiceResponse<AuditPagination> srvRes = null;
            rm.Setup(r => r.MapServiceResponse(
                It.Is<Type>(t => t == typeof(AuditPaginationModel)),
                It.IsAny<ServiceResponse>()))
                .Returns(new OkResult())
                .Callback<Type, ServiceResponse>((t2, s) => srvRes = s as ServiceResponse<AuditPagination>);
            var wc = new WorkContext { CurrentClientId = "1232" };
            var c = new AnyServiceConfig { MapperName = "default" };
            var ctrl = new AuditController(aSrv.Object, c, wc, rm.Object, _ecrs, l.Object);

            var res = await ctrl.GetAll(auditRecordTypes: AuditRecordTypes.CREATE);
            var jr = res.ShouldBeOfType<JsonResult>();
            var v = jr.Value.ShouldBeOfType<List<AuditRecordModel>>();

            v.Count().ShouldBe(page.Data.Count());
            v.ShouldAllBe(x => page.Data.Any(d => d.Id == x.Id));
        }
        [Fact]
        public async Task GetAll_ReturnsPage_Pagination()
        {
            var page = new AuditPagination
            {
                Data = new[]
                {
                    new AuditRecord{Id = "a"},
                    new AuditRecord{Id = "b"},
                    new AuditRecord{Id = "c"},
                    new AuditRecord{Id = "d"},
                }
            };
            var aSrv = new Mock<IAuditManager>();
            aSrv.Setup(c => c.GetAll(It.IsAny<AuditPagination>()))
                .ReturnsAsync(new ServiceResponse<AuditPagination> { Payload = page, Result = ServiceResult.Ok });

            var l = new Mock<ILogger<AuditController>>();
            var rm = new Mock<IServiceResponseMapper>();
            ServiceResponse<AuditPagination> srvRes = null;
            rm.Setup(r => r.MapServiceResponse(
                It.Is<Type>(t => t == typeof(AuditPaginationModel)),
                It.IsAny<ServiceResponse>()))
                .Returns(new OkResult())
                .Callback<Type, ServiceResponse>((t2, s) => srvRes = s as ServiceResponse<AuditPagination>);
            var wc = new WorkContext { CurrentClientId = "1232" };
            var c = new AnyServiceConfig { MapperName = "default" };
            var ctrl = new AuditController(aSrv.Object, c, wc, rm.Object, _ecrs, l.Object);

            var res = await ctrl.GetAll(dataOnly: false, auditRecordTypes: AuditRecordTypes.CREATE);
            var ok = res.ShouldBeOfType<OkResult>();
            srvRes.Payload.ShouldBe(page);
        }

        [Fact]
        public async Task GetAll_ReturnsPage_Pagination_WithProjection()
        {
            var page = new AuditPagination
            {
                Data = new[]
                {
                    new AuditRecord{Id = "a"},
                    new AuditRecord{Id = "b"},
                    new AuditRecord{Id = "c"},
                    new AuditRecord{Id = "d"},
                }
            };
            var aSrv = new Mock<IAuditManager>();
            aSrv.Setup(c => c.GetAll(It.IsAny<AuditPagination>()))
                .ReturnsAsync(new ServiceResponse<AuditPagination> { Payload = page, Result = ServiceResult.Ok });

            var l = new Mock<ILogger<AuditController>>();
            var rm = new Mock<IServiceResponseMapper>();
            ServiceResponse<AuditPagination> srvRes = null;
            rm.Setup(r => r.MapServiceResponse(
                It.Is<Type>(t => t == typeof(AuditPaginationModel)),
                It.IsAny<ServiceResponse>()))
                .Returns(new OkResult())
                .Callback<Type, ServiceResponse>((t2, s) => srvRes = s as ServiceResponse<AuditPagination>);
            var wc = new WorkContext { CurrentClientId = "1232" };
            var c = new AnyServiceConfig { MapperName = "default" };
            var ctrl = new AuditController(aSrv.Object, c, wc, rm.Object, _ecrs, l.Object);

            var res = await ctrl.GetAll(dataOnly: false, auditRecordTypes: AuditRecordTypes.CREATE, projectedFields: nameof(AuditRecordModel.EntityId));
            var ok = res.ShouldBeOfType<OkResult>();
        }
    }
}