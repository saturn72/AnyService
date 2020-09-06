using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Controllers;
using AnyService.Services;
using AnyService.Services.Audit;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Controllers
{
    public class AuditControllerTests
    {
        [Fact]
        public void ValidateRoute()
        {
            var type = typeof(AuditController<>);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("audit");
        }
        [Theory]
        [InlineData(nameof(AuditController<MyClass>.GetAll), "GET", "{auditRecordType}")]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(AuditController<>);
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
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("ttt")]
        public async Task GetAll_UnknownAuditRecordType_ReturnsBadRequest(string auditRecordType)
        {
            var l = new Mock<ILogger<AuditController<MyClass>>>();
            var ctrl = new AuditController<MyClass>(null, l.Object, null);

            var res = await ctrl.GetAll(auditRecordType: auditRecordType);
            res.ShouldBeOfType<BadRequestResult>();
        }
        [Theory]
        [InlineData(AuditRecordTypes.CREATE)]
        [InlineData(AuditRecordTypes.READ)]
        [InlineData(AuditRecordTypes.UPDATE)]
        [InlineData(AuditRecordTypes.DELETE)]
        public async Task GetAll_TypeIdNotAuditable__ReturnsBadRequest(string auditRecordType)
        {
            var l = new Mock<ILogger<AuditController<MyClass>>>();
            var ctrl = new AuditController<MyClass>(null, l.Object, null);

            var res = await ctrl.GetAll(auditRecordType: auditRecordType);
            res.ShouldBeOfType<BadRequestResult>();
        }
        public class MyAuditableClass : IDomainModelBase, ICreatableAudit
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task GetAll_ReturnsPage()
        {
            var page = new Pagination<MyAuditableClass>
            {
                Data = new[]
                {
                    new MyAuditableClass{Id = "a"},
                    new MyAuditableClass{Id = "b"},
                    new MyAuditableClass{Id = "c"},
                    new MyAuditableClass{Id = "d"},
                }
            };
            var aSrv = new Mock<IAuditService>();
            aSrv.Setup(c => c.GetAll(It.IsAny<string>(), It.IsAny<Pagination<MyAuditableClass>>())).ReturnsAsync(new ServiceResponse { Data = page, Result = ServiceResult.Ok });

            var l = new Mock<ILogger<AuditController<MyClass>>>();
            var ctrl = new AuditController<MyClass>(aSrv.Object, l.Object, null);

            var res = await ctrl.GetAll(auditRecordType: AuditRecordTypes.CREATE);

            var ok = res.ShouldBeOfType<OkObjectResult>();
            var v = ok.Value as PaginationModel<MyAuditableClass>;
            v.Data.Count().ShouldBe(page.Data.Count());
            for (int i = 0; i < v.Data.Count(); i++)
                page.Data.ShouldContain(x => x.Id == v.Data.ElementAt(i).Id);
        }

    }
}