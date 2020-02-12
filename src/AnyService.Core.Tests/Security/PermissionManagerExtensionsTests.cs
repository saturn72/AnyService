using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core.Security;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests.Security
{
    public class PermissionManagerExtensionsTests
    {
        static readonly string _uId = "userId",
            _ek = "e-key",
            _pk = "p-key",
            _eId = "e-Id";

        #region UserHasPermissionOnEntity
        [Fact]
        public async Task UserHasPermissionOnEntity_ReturnsTrue()
        {
            var userPermissions = new UserPermissions
            {
                EntityPermissions = new[]
                {
                    new EntityPermission
                    {
                        EntityId = _eId ,
                        EntityKey = _ek,
                        PermissionKeys = new[]{ _pk}
                    },
                },
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);

            var res = await PermissionManagerExtensions.UserHasPermissionOnEntity(pm.Object, _uId, _ek, _pk, _eId);
            res.ShouldBeTrue();
        }
        [Theory]
        [MemberData(nameof(UserHasPermissionOnEntity_ReturnsFalse_DATA))]
        public async Task UserHasPermissionOnEntity_ReturnsFalse(UserPermissions userPermissions)
        {
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(userPermissions);

            var res = await PermissionManagerExtensions.UserHasPermissionOnEntity(pm.Object, _uId, _ek, _pk, _eId);
            res.ShouldBeFalse();
        }

        public static IEnumerable<object[]> UserHasPermissionOnEntity_ReturnsFalse_DATA => new[]
        {
            new object[] { null as UserPermissions},
            new object[] { new  UserPermissions()},
            new object[]
            {
                new  UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = _eId + "www",
                            EntityKey = _ek,
                            PermissionKeys = new[]{ _pk}

                        },
                    },
                },
            },
            new object[]
            {
                new  UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = _eId,
                            EntityKey = _ek+ "www",
                            PermissionKeys = new[]{ _pk}

                        },
                    },
                },
            },
            new object[]
            {
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = _eId,
                            EntityKey = _ek,
                            PermissionKeys = new[]{ _pk+ "www"},
                        },
                    },
                },
            },    new object[]
            {
                new UserPermissions
                {
                    EntityPermissions = new []
                    {
                        new EntityPermission
                        {
                            EntityId = _eId,
                            EntityKey = _ek,
                            PermissionKeys = new[]{ _pk},
                            Excluded = true,
                        },
                    },
                },
            },
        };

        #endregion
    }
}