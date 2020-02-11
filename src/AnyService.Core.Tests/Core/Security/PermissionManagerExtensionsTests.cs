using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Core.Security;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests.Security
{
    public class PermissionManagerExtensionsTests
    {
        #region GetPermittedEntityIds
        [Theory]
        [MemberData(nameof(GetPermittedEntityIds_ReturnsEmptyCollection_DATA))]
        public async Task GetPermittedEntityIds_ReturnsEmptyCollection(UserPermissions userPermissions)
        {
            string userId = "some-user",
             entityKey = "entityKey",
             permissionKey = "permissionKey";
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);

            var ids = await pm.Object.GetPermittedEntityIds(userId, entityKey, permissionKey);
            ids.Count().ShouldBe(0);
        }
        public static IEnumerable<object[]> GetPermittedEntityIds_ReturnsEmptyCollection_DATA => new[]
        {
            new object[]{ null as UserPermissions},
            new object[]{ new UserPermissions() },
        };

        [Fact]
        public async Task GetPermittedEntityIds_ReturnsIds()
        {
            string userId = "some-user",
             entityKey = "entityKey",
             permissionKey = "permissionKey";
            var up = new UserPermissions
            {
                EntityPermissions = new[]{
                    new EntityPermission
                    {
                        EntityId = "a",
                        EntityKey = entityKey,
                        Excluded = true,
                        PermissionKeys = new[]{permissionKey, "pk2"},
                    },
                    new EntityPermission
                    {
                        EntityId = "b",
                        EntityKey = entityKey,
                        PermissionKeys = new[]{permissionKey, "pk2"},
                    },
                    new EntityPermission
                    {
                        EntityId = "c",
                        EntityKey = entityKey,
                        PermissionKeys = new[]{permissionKey, },
                    },
                    new EntityPermission
                    {
                        EntityId = "d",
                        EntityKey = entityKey,
                        PermissionKeys = new[]{"pk"},
                    },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);

            var ids = await pm.Object.GetPermittedEntityIds(userId, entityKey, permissionKey);
            ids.Count().ShouldBe(2);
            ids.ShouldContain("b");
            ids.ShouldContain("c");
        }
        #endregion
    }
}