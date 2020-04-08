using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Core.Security
{
    public class PermissionManagerExtensionsTests
    {
        private const string uId = "u-id",
             ek = "ek",
             pk = "pk",
             eId = "e-id";
        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsTrue()
        {
            var up = new UserPermissions
            {
                UserId = uId,
                EntityPermissions = new[]
                {
                        new EntityPermission
                        {
                            EntityId = eId,
                            EntityKey = ek,
                            PermissionKeys = new []{pk}
                        }
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);

            var res = await PermissionManagerExtensions.UserHasPermissionOnEntity(pm.Object, uId, ek, pk, eId);
            res.ShouldBeTrue();
        }
        [Theory]
        [MemberData(nameof(UserHasPermissionOnEntity_DATA))]
        public async Task UserHasPermissionOnEntity_ReturnsFalse(UserPermissions up)
        {
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);

            var res = await PermissionManagerExtensions.UserHasPermissionOnEntity(pm.Object, uId, ek, pk, eId);
            res.ShouldBeFalse();
        }

        public static IEnumerable<object[]> UserHasPermissionOnEntity_DATA =>
        new[]
        {
            new object[] { null as UserPermissions }, //null user permission
            new object[] { new UserPermissions() }, //has no EntityPermissions
            //has no EntityPermissions
            new object[] {
                new UserPermissions
                {
                    EntityPermissions = new EntityPermission[]{}
                }
            }, 
            //has no matching EntityPermissions
            new object[] {
                new UserPermissions
                {
                    UserId = uId ,
                    EntityPermissions = new []{
                        new EntityPermission
                        {
                            EntityId = eId+ "fail",
                            EntityKey = ek,
                            PermissionKeys = new []{pk}
                        }
                }
            },
            },
            //has no matching EntityPermissions
            new object[] {
                new UserPermissions
                {
                    UserId = uId ,
                    EntityPermissions = new []{
                        new EntityPermission
                        {
                            EntityId = eId,
                            EntityKey = ek+ "fail",
                            PermissionKeys = new []{pk}
                        }
                }
            },
          },
            //has no matching EntityPermissions
            new object[] {
                new UserPermissions
                {
                    UserId = uId ,
                    EntityPermissions = new []{
                        new EntityPermission
                        {
                            EntityId = eId,
                            EntityKey = ek,
                            PermissionKeys = new []{pk+ "fail"}
                        }
                }
            },
            },
            //excluded
            new object[] {
                new UserPermissions
                {
                    UserId = uId ,
                    EntityPermissions = new []{
                        new EntityPermission
                        {
                            Excluded = true,
                            EntityId = eId,
                            EntityKey = ek,
                            PermissionKeys = new []{pk}
                        }
                }
            },
            },
        };
        // public static async Task<bool> UserHasPermissionOnEntity
        //(this IPermissionManager manager, string userId, string entityKey, string permissionKey, string entityId)
        // {
        //     var userPermissions = await manager.GetUserPermissions(userId);
        //     var hasPermission = userPermissions?
        //         .EntityPermissions?
        //         .FirstOrDefault(p => p.EntityId.Equals(entityId, StringComparison.InvariantCultureIgnoreCase)
        //             && p.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase)
        //             && p.EntityKey.Equals(entityKey, StringComparison.InvariantCultureIgnoreCase));
        //     return !hasPermission?.Excluded ?? false;
        // }
    }
}
