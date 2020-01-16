using System.Threading.Tasks;
using AnyService.Core.Security;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services.Security
{
    public class PermissionManagerExtensionsTests
    {
        [Fact]
        public async Task UserIsGranted_Optimistic_ReturnNullUserPermissionsFromDatabase()
        {
            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            string userId = "uif",
                permissionKey = "pk",
                entityKey = "ek",
                entityId = "eid";
            var style = PermissionStyle.Optimistic;

            var res = await PermissionManagerExtensions.UserIsGranted(pm.Object, userId, permissionKey, entityKey, entityId, style);
            res.ShouldBeTrue();

        }
        [Fact]
        public async Task UserIsGranted_Optimistic_ReturnUserPermissions_Excluded_FromDatabase()
        {
            string userId = "uif",
                permissionKey = "pk",
                entityKey = "ek",
                entityId = "eid";
            var style = PermissionStyle.Optimistic;

            var up = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission {
                        Excluded = true ,
                        PermissionKey = permissionKey,
                        EntityKey = entityKey,
                        EntityId = entityId,
                        },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);

            var res = await PermissionManagerExtensions.UserIsGranted(pm.Object, userId, permissionKey, entityKey, entityId, style);
            res.ShouldBeFalse();
        }
        [Fact]
        public async Task UserIsGranted_Pesimistic_ReturnNullUserPermissionsFromDatabase()
        {
            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            string userId = "uif",
                permissionKey = "pk",
                entityKey = "ek",
                entityId = "eid";
            var style = PermissionStyle.Pesimistic;

            var res = await PermissionManagerExtensions.UserIsGranted(pm.Object, userId, permissionKey, entityKey, entityId, style);
            res.ShouldBeFalse();

        }
        [Fact]
        public async Task UserIsGranted_Pesimistic_ReturnUserPermissions_Excluded_FromDatabase()
        {
            string userId = "uif",
                permissionKey = "pk",
                entityKey = "ek",
                entityId = "eid";
            var style = PermissionStyle.Pesimistic;

            var up = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission {
                        Excluded = true ,
                        PermissionKey = permissionKey,
                        EntityKey = entityKey,
                        EntityId = entityId,
                        },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(pm => pm.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);

            var res = await PermissionManagerExtensions.UserIsGranted(pm.Object, userId, permissionKey, entityKey, entityId, style);
            res.ShouldBeTrue();
        }
    }
}