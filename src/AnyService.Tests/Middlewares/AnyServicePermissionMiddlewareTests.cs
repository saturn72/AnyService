using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core;
using AnyService.Core.Security;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class AnyServicePermissionMiddlewareTests
    {
        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        public async Task InvokeAsync_PermittedMethods(string method)
        {
            int i = 0, expValue = 15;
            RequestDelegate reqDel = hc =>
            {
                i = 15;
                return Task.CompletedTask;
            };
            var mw = new AnyServicePermissionMiddleware(reqDel, null);
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(TestModel),
                    PublicGet = true
                },
                RequestInfo = new RequestInfo
                {
                    Method = method
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            await mw.InvokeAsync(httpContext.Object, wc);
            i.ShouldBe(expValue);
        }

        [Theory]
        [InlineData("put")]
        [InlineData("delete")]
        public async Task InvokeAsync_BadRequestOnMissingIdForDeleteAndPut(string method)
        {
            var mw = new AnyServicePermissionMiddleware(null, null);
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(TestModel),
                },
                RequestInfo = new RequestInfo
                {
                    Method = method
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            await mw.InvokeAsync(httpContext.Object, wc);

            httpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status400BadRequest, Times.Once);
        }
        [Fact]
        public async Task IsGranted_NotSupportedMethod_ReturnsFalse()
        {
            var mw = new AnyServicePermissionMiddleware(null, null);
            var wc = new WorkContext
            {
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(TestModel),
                },
                RequestInfo = new RequestInfo
                {
                    Method = "not-supported-method",
                }
            };
            var httpResponse = new Mock<HttpResponse>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(h => h.Response).Returns(httpResponse.Object);
            await mw.InvokeAsync(httpContext.Object, wc);

            httpResponse.VerifySet(r => r.StatusCode = StatusCodes.Status400BadRequest, Times.Once);
        }
        #region IsGranted
        [Theory]
        [MemberData(nameof(CRUD_RetunsMockedAnswer_DATA))]
        public async Task CRUD_RetunsMockedAnswer(string method, UserPermissions userPermissions, bool isGranted)
        {
            var ecr = new EntityConfigRecord
            {
                EntityKey = nameof(TestModel),
                Type = typeof(TestModel),
                PermissionRecord = new PermissionRecord("create", "read", "update", "delete"),
                PublicGet = true,
            };

            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);
            var mw = new TestAnyServicePermissionware(null, pm.Object);
            var wc = new WorkContext
            {
                CurrentUserId = "userId",
                CurrentEntityConfigRecord = ecr,
                RequestInfo = new RequestInfo
                {
                    Method = method,
                    RequesteeId = "some-id",
                },
            };
            var res = await mw.IsGrantedForTest(wc);
            res.ShouldBe(isGranted);
        }
        public static IEnumerable<object[]> CRUD_RetunsMockedAnswer_DATA = new[]
        {
            new object[]
            {
                "get",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                            Excluded = true,
                        },
                    },
                },
                false
            },
            new object[]
            {
                "get",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                        },
                    },
                },
                true
            },
                        new object[]
            {
                "put",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                            Excluded = true,
                        },
                    },
                },
                false
            },
            new object[]
            {
                "put",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                        },
                    },
                },
                true
            },
            new object[]
            {
                "delete",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                            Excluded = true,
                        },
                    },
                },
                false
            },
            new object[]
            {
                "delete",
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = "some-id",
                            EntityKey = nameof(TestModel),
                            PermissionKeys = new[]{ "create", "read", "update", "delete",},
                        },
                    },
                },
                true
            },
        };

        #endregion
        public class TestModel : IDomainModelBase
        {
            public string Id { get; set; }
            public int Value { get; set; }
        }

        public class TestAnyServicePermissionware : AnyServicePermissionMiddleware
        {
            public TestAnyServicePermissionware(RequestDelegate next, IPermissionManager permissionManager) : base(next, permissionManager)
            {
            }

            public Task<bool> IsGrantedForTest(WorkContext workContext) => base.IsGranted(workContext);
        }
    }
}