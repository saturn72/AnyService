using AnyService.Http;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class WorkContextMiddlewareTests
    {
        [Fact]
        public async Task UserIdIsNull_ReturnUnauthroized()
        {
            var logger = new Mock<ILogger<WorkContextMiddleware>>();
            var ecr = new EntityConfigRecord
            {
                EndpointSettings = new EndpointSettings
                {
                    Route = "/some-resource",
                    GetSettings = new EndpointMethodSettings { Disabled = true },
                    PutSettings = new EndpointMethodSettings { Disabled = true },
                },
                Type = typeof(string),
            };
            var entityConfigRecords = new[] { ecr };

            var wcm = new WorkContextMiddleware(null, entityConfigRecords, logger.Object);

            var user = new Mock<ClaimsPrincipal>();
            user.Setup(u => u.Claims).Returns(new Claim[] { });
            var ctx = new Mock<HttpContext>();

            ctx.SetupGet(r => r.Request.Headers).Returns(new HeaderDictionary());
            var res = new Mock<HttpResponse>();
            ctx.Setup(h => h.User).Returns(user.Object);
            ctx.Setup(h => h.Response).Returns(res.Object);
            var wc = new WorkContext();
            await wcm.InvokeAsync(ctx.Object, wc);
            res.VerifySet(r => r.StatusCode = StatusCodes.Status401Unauthorized, Times.Once);
        }
        [Fact]
        public async Task ParseRequestInfoAndMoveToNext()
        {
            int i = 0,
                expI = 15;
            string expUserId = "user-id",
                route = "/some-resource/part2/",
                expRequesteeId = "123",
                expPath = $"{route}/__public/{expRequesteeId}",
                expMethod = "get";

            var ecr = new EntityConfigRecord
            {
                Name = "nane",
                EndpointSettings = new EndpointSettings
                {
                    Route = route,
                },
                Type = typeof(string),
            };
            var entityConfigRecords = new[] { ecr };
            RequestDelegate reqDel = hc =>
            {
                i = expI;
                return Task.CompletedTask;
            };

            var logger = new Mock<ILogger<WorkContextMiddleware>>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, expUserId) }));
            var ctx = new Mock<HttpContext>();
            var req = new Mock<HttpRequest>();
            req.Setup(r => r.Path).Returns(new PathString(expPath));
            req.Setup(r => r.Method).Returns(expMethod);
            req.SetupGet(r => r.Headers).Returns(new HeaderDictionary());

            var response = new Mock<HttpResponse>();
            ctx.Setup(h => h.User).Returns(user);
            ctx.Setup(h => h.Request).Returns(req.Object);
            ctx.Setup(h => h.Response).Returns(response.Object);
            var wc = new WorkContext();

            var wcm = new WorkContextMiddleware(reqDel, entityConfigRecords, logger.Object);
            await wcm.InvokeAsync(ctx.Object, wc);
            i.ShouldBe(expI);
            wc.CurrentEntityConfigRecord.ShouldBe(ecr);
            wc.RequestInfo.Path.ShouldBe(expPath);
            wc.RequestInfo.Method.ShouldBe(expMethod);
            wc.RequestInfo.RequesteeId.ShouldBe(expRequesteeId);
        }
        [Fact]
        public void BuildDisabledMethodMap()
        {
            var e = new EntityConfigRecord
            {
                Name = "name",
                EndpointSettings = new EndpointSettings
                {
                    GetSettings = new EndpointMethodSettings { Disabled = true },
                    PutSettings = new EndpointMethodSettings { Disabled = true },
                }
            };

            var wcmt = new WorkContextMiddleware_ForTests(new[] { e });
            wcmt.ActiveMap[$"{e.Name}_post"].ShouldBeFalse();
            wcmt.ActiveMap[$"{e.Name}_get"].ShouldBeTrue();
            wcmt.ActiveMap[$"{e.Name}_put"].ShouldBeTrue();
            wcmt.ActiveMap[$"{e.Name}_delete"].ShouldBeFalse();
        }

        [Fact]
        public async Task ExtractsHttpContext()
        {
            string expUserId = "u-id",
                expClientId = "c-Id",
                expSessionId = "s-id",
                expRefId = "r-id";
            var hd = new HeaderDictionary
            {
                { HttpHeaderNames.ClientSessionId, expSessionId },
                { HttpHeaderNames.ClientRequestReference, expRefId },
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, expUserId),
                new Claim("client_id", expClientId),
            }));
            var ctx = new Mock<HttpContext>();
            var req = new Mock<HttpRequest>();
            req.Setup(r => r.Path).Returns(new PathString("/do/123"));
            req.Setup(r => r.Method).Returns("get");
            req.SetupGet(r => r.Headers).Returns(hd);
            var response = new Mock<HttpResponse>();
            ctx.Setup(h => h.User).Returns(user);
            ctx.Setup(h => h.Request).Returns(req.Object);

            var mw = new WorkContextMiddleware_ForTests(new EntityConfigRecord[] { });

            var wc = new WorkContext();
            var res = await mw.ExtractHttpContext(ctx.Object, wc);
            res.ShouldBeTrue();

            wc.CurrentUserId.ShouldBe(expUserId);
            wc.CurrentClientId.ShouldBe(expClientId);
            wc.SessionId.ShouldBe(expSessionId);
            wc.ReferenceId.ShouldBe(expRefId);
        }

        public class WorkContextMiddleware_ForTests : WorkContextMiddleware
        {
            public WorkContextMiddleware_ForTests(IEnumerable<EntityConfigRecord> entityConfigRecords)
                : base(
                      null,
                      entityConfigRecords,
                      new Mock<ILogger<WorkContextMiddleware_ForTests>>().Object,
                      null)
            {
            }
            public Task<bool> ExtractHttpContext(HttpContext ctx, WorkContext wc) => HttpContextToWorkContext(ctx, wc);
            public IReadOnlyDictionary<string, bool> ActiveMap => base.ActivationMaps;
        }
    }
}