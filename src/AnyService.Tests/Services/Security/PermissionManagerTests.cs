using AnyService.Core.Caching;
using AnyService.Core.Security;
using AnyService.Services.Security;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.Security
{
    public class PermissionManagerTests
    {
        #region UserHasPermissions
        [Theory]
        [InlineData("", "pk")]
        [InlineData(" ", "pk")]
        [InlineData(null, "pk")]
        [InlineData("ui", "")]
        [InlineData("ui", " ")]
        [InlineData("ui", null)]
        public async Task UserHasPermission_MissingValues(string userId, string permissionKey)
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(null as IEnumerable<UserPermissions>);
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.GetUserPermission(userId, permissionKey);
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserHasPermission_ReturnsNullfromDB()
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(null as IEnumerable<UserPermissions>);
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.GetUserPermission("user-id", "pk");
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserHasPermission_ReturnsEmptycollectionFromDB()
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new UserPermissions[] { });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.GetUserPermission("user-id", "pk");
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserHasPermission_ReturnsNonMatchingPermissionKey()
        {
            var up = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission
                {
                    PermissionKey = "asdf"
                }}
            };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new[] { up });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.GetUserPermission("user-id", "pk");
            res.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasPermission_ReturnsTrue()
        {
            var pk = "per-key";
            var up = new UserPermissions
            {
                EntityPermissions = new[]
               {
                    new EntityPermission
                {
                    PermissionKey = pk
                }}
            };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new[] { up });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.GetUserPermission("user-id", pk);
            res.ShouldBeTrue();
        }
        #endregion
        #region UserHasPermissionOnEntity

        [Theory]
        [InlineData("", "pk", "ek", "ei")]
        [InlineData(" ", "pk", "ek", "ei")]
        [InlineData(null, "pk", "ek", "ei")]
        [InlineData("ui", "", "ek", "ei")]
        [InlineData("ui", " ", "ek", "ei")]
        [InlineData("ui", null, "ek", "ei")]
        [InlineData("ui", "pk", "", "ei")]
        [InlineData("ui", "pk", " ", "ei")]
        [InlineData("ui", "pk", null, "ei")]
        [InlineData("ui", "pk", "ek", "")]
        [InlineData("ui", "pk", "ek", " ")]
        [InlineData("ui", "pk", "ek", null)]
        public async Task UserHasPermissionOnEntity_MissingValues(string userId, string permissionKey, string entityKey, string entityId)
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(null as IEnumerable<UserPermissions>);
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.UserHasPermissionOnEntity(userId, permissionKey, entityKey, entityId);
            res.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsNullfromDB()
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(null as IEnumerable<UserPermissions>);
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.UserHasPermissionOnEntity("user-id", "pk", "ek", "eid");
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsEmptycollectionFromDB()
        {
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new UserPermissions[] { });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.UserHasPermissionOnEntity("user-id", "pk", "ek", "eid");
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsNonMatchingPermissionKey()
        {
            var up = new UserPermissions
            {
                EntityPermissions = new[]
               {
                    new EntityPermission
                {
                    PermissionKey = "asdf"
                }}
            };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new[] { up });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.UserHasPermissionOnEntity("user-id", "pk", "ek", "eid");
            res.ShouldBeFalse();
        }

        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsTrue()
        {
            string pk = "per-key",
                ek = "ek",
                eid = "eid";

            var up = new UserPermissions
            {
                EntityPermissions = new[]
                           {
                    new EntityPermission
                {
                    PermissionKey = pk ,
                    EntityKey = ek,
                    EntityId = eid,
                }}
            };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.GetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<IEnumerable<UserPermissions>>>>(),
                It.IsAny<TimeSpan>())).ReturnsAsync(new[] { new UserPermissions {
                } });
            var pm = new PermissionManager(cm.Object, null);
            var res = await pm.UserHasPermissionOnEntity("user-id", pk, ek, eid);
            res.ShouldBeTrue();
        }
        #endregion
    }
}
