using System.Reflection;
using AnyService.Controllers;
using AnyService.Logging;
using AnyService.Services.Logging;
using AnyService.Tests.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace AnyService.Tests.Controllers
{
    public class LogRecordControllerTests : MappingTest
    {
        [Fact]
        public void ValidateRouteAndRole()
        {
            var type = typeof(LogRecordController);
            var route = type.GetCustomAttributes(typeof(RouteAttribute)).First() as RouteAttribute;
            route.Template.ShouldBe("__log");
            var auth = type.GetCustomAttributes(typeof(AuthorizeAttribute)).First() as AuthorizeAttribute;
            auth.Roles.ShouldBe("log-record-read");
        }
        [Theory]
        [InlineData(nameof(LogRecordController.GetAll), "GET", null)]
        public void ValidateVerbs(string methodName, string expHttpVerb, string expTemplate)
        {
            var type = typeof(LogRecordController);
            var mi = type.GetMethod(methodName);
            var att = mi.GetCustomAttributes(typeof(HttpMethodAttribute)).First() as HttpMethodAttribute;
            att.HttpMethods.First().ShouldBe(expHttpVerb);
            att.Template.ShouldBe(expTemplate);
        }

        [Theory]
        [InlineData("ttt")]
        [InlineData("ttt, " + LogRecordLevel.Debug)]
        public async Task GetAll_UnknownLogRecordType_ReturnsBadRequest(string LogRecordTypes)
        {
            var l = new Mock<ILogger<LogRecordController>>();
            var ctrl = new LogRecordController(null, l.Object);

            var res = await ctrl.GetAll(logLevels: LogRecordTypes);
            res.ShouldBeOfType<BadRequestResult>();
        }
        [Theory]
        [InlineData("ttt", null)]
        [InlineData(null, "ttt")]
        [InlineData("ttt", "rrr")]
        public async Task GetAll_InvalidFrom_Todates_ReturnsBadRequest(string fromUtc, string toUtc)
        {
            var l = new Mock<ILogger<LogRecordController>>();
            var ctrl = new LogRecordController(null, l.Object);

            var res = await ctrl.GetAll(fromUtc: fromUtc, toUtc: toUtc);
            res.ShouldBeOfType<BadRequestResult>();
        }
        [Fact]
        public async Task GetAll_ReturnsPage()
        {
            var page = new LogRecordPagination
            {
                Data = new[]
                {
                    new LogRecord{Id = "a"},
                    new LogRecord{Id = "b"},
                    new LogRecord{Id = "c"},
                    new LogRecord{Id = "d"},
                }
            };
            var lm = new Mock<ILogRecordManager>();
            lm.Setup(c => c.GetAll(It.IsAny<LogRecordPagination>()))
                .ReturnsAsync(page);

            var l = new Mock<ILogger<LogRecordController>>();
            var ctrl = new LogRecordController(lm.Object, l.Object);

            var res = await ctrl.GetAll(logLevels: LogRecordLevel.Information);

            var ok = res.ShouldBeOfType<OkObjectResult>();
            var v = ok.Value as LogRecordPagination;
            v.Data.Count().ShouldBe(page.Data.Count());
            for (int i = 0; i < v.Data.Count(); i++)
                page.Data.ShouldContain(x => x.Id == v.Data.ElementAt(i).Id);
        }

    }
}