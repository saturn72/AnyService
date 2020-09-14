using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Controllers;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Tests.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class AuditControllerTests : MappingTest
    {
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
        public class MyClass : IDomainModelBase
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
            var ctrl = new AuditController(null, l.Object, wc, null);

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
            var ctrl = new AuditController(null, l.Object, null, null);

            var res = await ctrl.GetAll(fromUtc: fromUtc, toUtc: toUtc);
            res.ShouldBeOfType<BadRequestResult>();
        }
        public class MyAuditableClass : IDomainModelBase, ICreatableAudit
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
                .ReturnsAsync(new ServiceResponse { Data = page, Result = ServiceResult.Ok });

            var l = new Mock<ILogger<AuditController>>();
            var rm = new Mock<IServiceResponseMapper>();
            ServiceResponse srvRes = null;
            rm.Setup(r => r.MapServiceResponse(It.IsAny<ServiceResponse>()))
                .Returns(new OkResult())
                .Callback<ServiceResponse>(s => srvRes = s);
            var wc = new WorkContext { CurrentClientId = "1232" };
            var ctrl = new AuditController(aSrv.Object, l.Object, wc, rm.Object);

            var res = await ctrl.GetAll(auditRecordTypes: AuditRecordTypes.CREATE);

            var v = srvRes.Data as PaginationModel<AuditRecord>;
            v.Data.Count().ShouldBe(page.Data.Count());
            for (int i = 0; i < v.Data.Count(); i++)
                page.Data.ShouldContain(x => x.Id == v.Data.ElementAt(i).Id);
        }

    }
}