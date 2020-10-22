using System.Threading.Tasks;
using AnyService.Security;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class AnyServicePermissionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_PublicGet()
        {
            int i = 0, expValue = 15;
            RequestDelegate reqDel = hc =>
            {
                i = 15;
                return Task.CompletedTask;
            };
            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new AnyServicePermissionMiddleware(reqDel, logger.Object);
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    PublicGet = true,
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                    },
                },
                RequestInfo = new RequestInfo
                {
                    Path = "/__public",
                    Method = "get",
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);

            await mw.InvokeAsync(httpContext.Object, wc, null);
            i.ShouldBe(expValue);
        }
        [Fact]
        public async Task InvokeAsync_PermittedMethods_Post()
        {
            int i = 0, expValue = 15;
            RequestDelegate reqDel = hc =>
            {
                i = 15;
                return Task.CompletedTask;
            };
            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new AnyServicePermissionMiddleware(reqDel, logger.Object);
            var createPermissionKey = "create-key";
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                        PermissionRecord = new PermissionRecord(createPermissionKey, null, null, null),
                    },
                },
                RequestInfo = new RequestInfo
                {
                    Method = "post",
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            var userPermissions = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission
                    {
                        EntityKey = createPermissionKey
                    }
                }
            };
            var mgr = new Mock<IPermissionManager>();
            mgr.Setup(m => m.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);
            await mw.InvokeAsync(httpContext.Object, wc, mgr.Object);
            i.ShouldBe(expValue);
        }

        [Theory]
        [InlineData("put", null)]
        [InlineData("delete", "123")]
        public async Task InvokeAsync_BadRequestOnMissingIdForDeleteAndPut(string method, string reqesteeId)
        {
            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new AnyServicePermissionMiddleware(null, logger.Object);
            string entityKey = "entity-key",
                updateKey = "update-key",
                deleteKey = "delete-key";

            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                        PermissionRecord = new PermissionRecord(null, null, updateKey, deleteKey),
                        EntityKey = entityKey,
                    },
                },
                RequestInfo = new RequestInfo
                {
                    Method = method,
                    RequesteeId = reqesteeId,
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);

            var userPermissions = new UserPermissions();
            var mgr = new Mock<IPermissionManager>();
            mgr.Setup(m => m.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);
            await mw.InvokeAsync(httpContext.Object, wc, mgr.Object);

            httpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status403Forbidden, Times.Once);
        }
        [Fact]
        public async Task IsGranted_NotSupportedMethod_ReturnsFalse()
        {
            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new AnyServicePermissionMiddleware(null, logger.Object);
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    EntityConfigRecord = new EntityConfigRecord
                    {
                        Type = typeof(TestModel),
                    },
                },
                RequestInfo = new RequestInfo
                {
                    Method = "not-supported-method",
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            await mw.InvokeAsync(httpContext.Object, wc, null);

            httpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status403Forbidden, Times.Once);
        }
        #region IsGranted
        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task CRUD_ReturnsTrue_OnPost_Or_OnPublicGet(string method)
        {
            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new TestAnyServicePermissionware(null, logger.Object);
            var wc = new WorkContext
            {
                CurrentEndpointSettings = new EndpointSettings
                {
                    PublicGet = true,
                    EntityConfigRecord = new EntityConfigRecord
                    {
                    },
                },
                RequestInfo = new RequestInfo
                {
                    Method = method,
                    Path = "/",
                },
            };
            var res = await mw.IsGrantedForTest(wc, null);
            res.ShouldBeTrue();
        }
        [Theory]
        [InlineData("get", "some-entity-id", false, true)]
        [InlineData("get", "some-entity-id", true, false)]
        [InlineData("get", "diff-entity-id", false, false)]
        [InlineData("get", "diff-entity-id", true, false)]
        [InlineData("put", "some-entity-id", false, true)]
        [InlineData("put", "some-entity-id", true, false)]
        [InlineData("put", "diff-entity-id", false, false)]
        [InlineData("put", "diff-entity-id", true, false)]
        [InlineData("delete", "some-entity-id", false, true)]
        [InlineData("delete", "some-entity-id", true, false)]
        [InlineData("delete", "diff-entity-id", false, false)]
        [InlineData("delete", "diff-entity-id", true, false)]
        public async Task CRUD_ReturnsExpResult(string method, string requesteeId, bool excluded, bool expGrantResult)
        {
            var ecr = new EndpointSettings
            {
                PublicGet = true,
                EntityConfigRecord = new EntityConfigRecord
                {
                    EntityKey = nameof(TestModel),
                    Type = typeof(TestModel),
                    PermissionRecord = new PermissionRecord("create", "read", "update", "delete"),
                },
            };
            var userPermissions = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission
                    {
                        Excluded = excluded,
                        EntityId = "some-entity-id",
                        EntityKey = nameof(TestModel),
                        PermissionKeys = new[]{ "create", "read", "update", "delete",},
                    },
                },
            };

            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);

            var logger = new Mock<ILogger<AnyServicePermissionMiddleware>>();
            var mw = new TestAnyServicePermissionware(null, logger.Object);
            var wc = new WorkContext
            {
                CurrentUserId = "userId",
                CurrentEndpointSettings = ecr,
                RequestInfo = new RequestInfo
                {
                    Method = method,
                    RequesteeId = requesteeId,
                },
            };
            var res = await mw.IsGrantedForTest(wc, pm.Object);
            res.ShouldBe(expGrantResult);
        }

        #endregion
        public class TestModel : IEntity
        {
            public string Id { get; set; }
            public int Value { get; set; }
        }

        public class TestAnyServicePermissionware : AnyServicePermissionMiddleware
        {
            public TestAnyServicePermissionware(RequestDelegate next, ILogger<AnyServicePermissionMiddleware> logger) : base(next, logger)
            {
            }

            public Task<bool> IsGrantedForTest(WorkContext workContext, IPermissionManager permissionManager) => base.IsGranted(workContext, permissionManager);
        }
    }
}
