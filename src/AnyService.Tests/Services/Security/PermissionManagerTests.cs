using AnyService.Core.Caching;
using AnyService.Core.Security;
using AnyService.Services;
using AnyService.Services.Security;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Security
{
    public class PermissionManagerTests
    {

        #region GetUserPermissions
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetUserPermissions_UserIdHasNoValue(string userId)
        {
            var pm = new PermissionManager(null, null);
            var up = await pm.GetUserPermissions(userId);
            up.ShouldBeNull();
        }
        [Fact]
        public async Task GetUserPermissions_ReturnFromCache()
        {
            var userId = "some-user";
            var expUserPermissions = new UserPermissions();
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync<UserPermissions>(It.IsAny<string>())).ReturnsAsync(expUserPermissions);
            var pm = new PermissionManager(cm.Object, null);

            var res = await pm.GetUserPermissions(userId);
            res.ShouldBe(expUserPermissions);
        }
        [Fact]
        public async Task GetUserPermissions_ReturnNullFromDb()
        {
            var userId = "some-user";
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync<UserPermissions>(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var repo = new Mock<IRepository<UserPermissions>>();
            repo.Setup(r => r.GetAll(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);

            var pm = new PermissionManager(cm.Object, repo.Object);

            var res = await pm.GetUserPermissions(userId);
            res.ShouldBeNull();
        }
        [Fact]
        public async Task GetUserPermissions_ReturnFromDb_CachesData()
        {
            var userId = "some-user";
            var expUserPermissions = new UserPermissions();
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync<UserPermissions>(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var repo = new Mock<IRepository<UserPermissions>>();
            repo.Setup(r => r.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(expUserPermissions);

            var pm = new PermissionManager(cm.Object, repo.Object);

            var res = await pm.GetUserPermissions(userId);
            cm.Verify(c => c.SetAsync(
                    It.Is<string>(s => s.EndsWith(userId)),
                    It.Is<UserPermissions>(u => u == expUserPermissions),
                    It.IsAny<TimeSpan>()),
                Times.Once);
            res.ShouldBe(expUserPermissions);
        }

        #endregion

        #region CreateUserPermissions
        [Fact]
        public async Task CreateUserPermissions_All_TESTS_ARE_MISSING()
        {
            throw new System.NotImplementedException("add crate unit tests");
        }
        #endregion
    }
}