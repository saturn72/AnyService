using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Security;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Conventions.Security
{
    public class PermissionManagerExtensionsTests
    {
        #region GetPermittedIds
        [Theory]
        [MemberData(nameof(GetPermittedIds_HasUserPermissions_ReturnsEmpty_DATA))]
        public async Task GetPermittedIds_HasUserPermissions_ReturnsEmpty(UserPermissions userPermissions)
        {
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);
            var res = await PermissionManagerExtensions.GetPermittedEntitiesIds(pm.Object, "uId", "ek", "pk");
            res.ShouldBeEmpty();
        }
        public static IEnumerable<object[]> GetPermittedIds_HasUserPermissions_ReturnsEmpty_DATA => new[]
        {
            new object[]{ null as  UserPermissions},
            new object[]{  new UserPermissions{ }},
            new object[]{
                new UserPermissions
                {
                    EntityPermissions = new[] { new EntityPermission { EntityKey = "ek-not-match" } }
                }
            },
            new object[]{
                new UserPermissions
                {
                    EntityPermissions = new[] { new EntityPermission {EntityKey = "ek",PermissionKeys = new string[]{},}},
                },
              },
               new object[]{
                new UserPermissions
                {
                    EntityPermissions = new[] { new EntityPermission { Excluded = true, EntityKey = "ek",PermissionKeys = new []{"pk"},}},
                },
              }

        };
        [Fact]
        public async Task GetPermittedIds_NotMatch_AllConditions_ReturnsEmpty()
        {
            var up = new UserPermissions
            {
                EntityPermissions = new[] { new EntityPermission { EntityKey = "ek-not-match" } }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var res = await PermissionManagerExtensions.GetPermittedEntitiesIds(pm.Object, "some-user-id", "ek", "pk");
            res.ShouldBeEmpty();
        }
        [Fact]
        public async Task GetPermittedIds_ReturnsEntityIds()
        {
            string eId1 = "id-1",
                eId2 = "id-2";

            var up = new UserPermissions
            {
                EntityPermissions = new[] {
                    new EntityPermission { EntityId = eId1,  EntityKey = "ek", PermissionKeys = new[] { "pk" }, } ,
                    new EntityPermission { EntityId = eId2, EntityKey = "ek", PermissionKeys = new[] { "pk" }, } ,
                    },
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var res = await PermissionManagerExtensions.GetPermittedEntitiesIds(pm.Object, "some-user-id", "ek", "pk");
            res.Count().ShouldBe(2);
            res.Contains(eId1);
            res.Contains(eId2);
        }

        #endregion
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
