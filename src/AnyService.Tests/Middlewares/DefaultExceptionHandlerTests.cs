using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnyService.Events;
using AnyService.Middlewares;
using AnyService.Utilities;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class DefaultExceptionHandlerTests
    {
        WorkContext wc = new WorkContext { CurrentUserId = "123" };

        string actualBody = "",
            exId = "ex-id",
            ek = "event-key",
            expPath = "exp-path";
        Exception expEx = new Exception();
        [Fact]
        public async Task HappyFlow()
        {

            var i = new Mock<IIdGenerator>();
            i.Setup(ig => ig.GetNext()).Returns(exId);
            var l = new Mock<ILogger<DefaultExceptionHandler>>();
            var eb = new Mock<IEventBus>();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var h = new DefaultExceptionHandler(i.Object, l.Object, eb.Object, sp.Object);

            var ctx = new Mock<HttpContext>();

            var ehpf = new Mock<IExceptionHandlerPathFeature>();
            ehpf.SetupGet(e => e.Path).Returns(expPath);
            ehpf.SetupGet(e => e.Error).Returns(expEx);

            var fc = new Mock<IFeatureCollection>();
            fc.Setup(f => f.Get<IExceptionHandlerPathFeature>()).Returns(ehpf.Object);
            ctx.SetupGet(c => c.Features).Returns(fc.Object);

            var req = new Mock<HttpRequest>();
            var res = new Mock<HttpResponse>();
            res.Setup(_ => _.Body.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Callback((byte[] data, int offset, int length, CancellationToken token) => actualBody = Encoding.UTF8.GetString(data));
            ctx.SetupGet(c => c.Request).Returns(req.Object);
            ctx.SetupGet(c => c.Response).Returns(res.Object);
            await h.Handle(ctx.Object, ek);

            actualBody.ShouldBe($"{{\"exeptionId\":\"{exId}\"}}");
            res.VerifySet(r => r.StatusCode = StatusCodes.Status500InternalServerError);
            res.VerifySet(r => r.ContentType = "text/json");
            eb.Verify(e => e.Publish(It.Is<string>(s => s == ek), It.Is<DomainEventData>(ed => VerifyDomainEventData(ed, req.Object))), Times.Once);

        }

        private bool VerifyDomainEventData(DomainEventData ed, HttpRequest req)
        {
            var res = ed.GetPropertyValueByName<string>("PerformedByUserId") == wc.CurrentUserId;
            var data = ed.GetPropertyValueByName<object>("Data");
            res = res && data.GetPropertyValueByName<WorkContext>("workContext") == wc;
            res = res && data.GetPropertyValueByName<string>("exceptionId") == exId;
            res = res && data.GetPropertyValueByName<Exception>("exception") == expEx;
            var rd = data.GetPropertyValueByName<object>("requestData");
            res = res && rd.GetPropertyValueByName<string>("Path") == expPath;
            res = res && rd.GetPropertyValueByName<HttpRequest>("Request") == req;
            return res;
        }
    }
}
