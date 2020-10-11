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
                    PostSettings = new EndpointMethodSettings { Active = true },
                    GetSettings = new EndpointMethodSettings { Active = false },
                    PutSettings = new EndpointMethodSettings { Active = false },
                    DeleteSettings = new EndpointMethodSettings { Active = true },
                },
                Type = typeof(string),
            };
            var entityConfigRecords = new[] { ecr };
            var wcm = new WorkContextMiddleware(null, entityConfigRecords, logger.Object);

            var user = new Mock<ClaimsPrincipal>();
            user.Setup(u => u.Claims).Returns(new Claim[] { });
            var hc = new Mock<HttpContext>();
            var hr = new Mock<HttpResponse>();
            hc.Setup(h => h.User).Returns(user.Object);
            hc.Setup(h => h.Response).Returns(hr.Object);
            await wcm.InvokeAsync(hc.Object, null);
            hr.VerifySet(r => r.StatusCode = StatusCodes.Status401Unauthorized, Times.Once);
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
                Identifier = "nane",
                EndpointSettings = new EndpointSettings
                {
                    Route = route,
                    PostSettings = new EndpointMethodSettings { Active = true },
                    GetSettings = new EndpointMethodSettings { Active = true },
                    PutSettings = new EndpointMethodSettings { Active = true },
                    DeleteSettings = new EndpointMethodSettings { Active = true },
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
            var request = new Mock<HttpRequest>();
            request.Setup(r => r.Path).Returns(new PathString(expPath));
            request.Setup(r => r.Method).Returns(expMethod);

            var response = new Mock<HttpResponse>();
            ctx.Setup(h => h.User).Returns(user);
            ctx.Setup(h => h.Request).Returns(request.Object);
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
        public void BuildActivationMap()
        {
            var e = new EntityConfigRecord
            {
                Identifier = "name",
                EndpointSettings = new EndpointSettings
                {
                    PostSettings = new EndpointMethodSettings { Active = true },
                    GetSettings = new EndpointMethodSettings { Active = false },
                    PutSettings = new EndpointMethodSettings { Active = false },
                    DeleteSettings = new EndpointMethodSettings { Active = true },
                }
            };
            var wcmt = new WorkContextMiddleware_ForTests(new[] { e });
            wcmt.ActiveMap[$"{e.Identifier}_post"].ShouldBeTrue();
            wcmt.ActiveMap[$"{e.Identifier}_get"].ShouldBeFalse();
            wcmt.ActiveMap[$"{e.Identifier}_put"].ShouldBeFalse();
            wcmt.ActiveMap[$"{e.Identifier}_delete"].ShouldBeTrue();
        }
        public class WorkContextMiddleware_ForTests : WorkContextMiddleware
        {
            public WorkContextMiddleware_ForTests(IEnumerable<EntityConfigRecord> entityConfigRecords)
                : base(null, entityConfigRecords, null, null)
            {
            }
            public IReadOnlyDictionary<string, bool> ActiveMap => base.ActivationMaps;
        }
    }
}