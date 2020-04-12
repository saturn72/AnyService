using AnyService.Core.Caching;
using AnyService.Core.Security;
using AnyService.Services;
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
            cm.Setup(c => c.Get<UserPermissions>(It.IsAny<string>())).ReturnsAsync(expUserPermissions);
            var pm = new PermissionManager(cm.Object, null);

            var res = await pm.GetUserPermissions(userId);
            res.ShouldBe(expUserPermissions);
        }
        [Theory]
        [MemberData(nameof(GetUserPermissions_ReturnEmptyOrReturnNullFromDb_DATA))]
        public async Task GetUserPermissions_ReturnEmptyOrReturnNullFromDb(IEnumerable<UserPermissions> data)
        {
            var userId = "some-user";
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get<UserPermissions>(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var repo = new Mock<IRepository<UserPermissions>>();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<UserPermissions>>())).ReturnsAsync(new Paginate<UserPermissions> { Data = data });

            var pm = new PermissionManager(cm.Object, repo.Object);

            var res = await pm.GetUserPermissions(userId);
            res.ShouldBeNull();
        }
        public static IEnumerable<object[]> GetUserPermissions_ReturnEmptyOrReturnNullFromDb_DATA => new[]
        {
            new object[]{null as IEnumerable<UserPermissions>},
            new object[]{new UserPermissions[]{}},
        };

        [Fact]
        public async Task GetUserPermissions_ReturnFromDb_And_CacheData()
        {
            var userId = "some-user";
            var up = new UserPermissions();
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get<UserPermissions>(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var repo = new Mock<IRepository<UserPermissions>>();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<UserPermissions>>())).ReturnsAsync(new Paginate<UserPermissions> { Data = new[] { up } });

            var pm = new PermissionManager(cm.Object, repo.Object);

            var res = await pm.GetUserPermissions(userId);
            cm.Verify(c => c.Set(
                    It.Is<string>(s => s.EndsWith(userId)),
                    It.Is<UserPermissions>(u => u == up),
                    It.IsAny<TimeSpan>()),
                Times.Once);
            res.ShouldBe(up);
        }

        #endregion

        #region CreateUserPermissions
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task CreateUserPermissions_UserIdHasNoValue(string userId)
        {
            var toCreate = new UserPermissions
            {
                UserId = userId
            };
            var pm = new PermissionManager(null, null);
            var up = await pm.CreateUserPermissions(toCreate);
            up.ShouldBeNull();
        }
        [Fact]
        public async Task CreateUserPermissions_PersistModel()
        {
            var repo = new Mock<IRepository<UserPermissions>>();
            var cm = new Mock<ICacheManager>();

            var userId = "some-user";
            var toCreate = new UserPermissions
            {
                UserId = userId
            };

            var pm = new PermissionManager(cm.Object, repo.Object);
            var up = await pm.CreateUserPermissions(toCreate);
            cm.Verify(c => c.Remove(It.Is<string>(s => s.EndsWith(userId))), Times.Once);
            repo.Verify(c => c.Insert(It.Is<UserPermissions>(s => s.UserId == userId)), Times.Once);
        }
        #endregion
        #region Update
        [Theory]
        [MemberData(nameof(CreateUserPermissions_InvalidModel_DATA))]
        public async Task CreateUserPermissions_InvalidModel(UserPermissions data)
        {
            var pm = new PermissionManager(null, null);
            var up = await pm.UpdateUserPermissions(data);
            up.ShouldBeNull();
        }
        public static IEnumerable<object[]> CreateUserPermissions_InvalidModel_DATA => new[]
        {
            new object[]{ null as UserPermissions},
            new object[]{ new UserPermissions()},
        };
        [Theory]
        [MemberData(nameof(UpdateUserPermissions_EntityDoesNotExistsInDatabase_DATA))]
        public async Task UpdateUserPermissions_EntityDoesNotExistsInDatabase(IEnumerable<UserPermissions> data)
        {
            var repo = new Mock<IRepository<UserPermissions>>();
            var cm = new Mock<ICacheManager>();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<UserPermissions>>())).ReturnsAsync(new Paginate<UserPermissions> { Data = data });

            var userId = "some-user";
            var toCreate = new UserPermissions
            {
                Id = "123",
                UserId = userId
            };

            var pm = new PermissionManager(cm.Object, repo.Object);
            var up = await pm.UpdateUserPermissions(toCreate);
            up.ShouldBeNull();
            cm.Verify(c => c.Remove(It.Is<string>(s => s.EndsWith(userId))), Times.Never);
        }
        public static IEnumerable<object[]> UpdateUserPermissions_EntityDoesNotExistsInDatabase_DATA => new[]
        {
            new object[]{ null as  IEnumerable<UserPermissions>},
            new object[]{ new UserPermissions[]{}},
        };
        [Fact]
        public async Task UpdateUserPermissions_Success()
        {
            var userId = "uid";
            var ep = new[]{
                new EntityPermission
            {
                EntityId = "eid",
            }};
            var toUpdate = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = ep,
            };
            var dbEntity = new UserPermissions
            {
                UserId = userId,
            };

            var repo = new Mock<IRepository<UserPermissions>>();
            var cm = new Mock<ICacheManager>();
            repo.Setup(r => r.GetAll(It.IsAny<Paginate<UserPermissions>>())).ReturnsAsync(new Paginate<UserPermissions> { Data = new[] { dbEntity } });
            repo.Setup(r => r.Update(It.IsAny<UserPermissions>())).ReturnsAsync(dbEntity);

            var pm = new PermissionManager(cm.Object, repo.Object);
            var res = await pm.UpdateUserPermissions(toUpdate);
            res.ShouldBe(dbEntity);

            cm.Verify(c => c.Remove(It.Is<string>(s => s.EndsWith(userId))), Times.Once);
            repo.Verify(r => r.Update(It.Is<UserPermissions>(u => u == dbEntity && u.EntityPermissions == ep)), Times.Once);
        }
        #endregion
    }
}