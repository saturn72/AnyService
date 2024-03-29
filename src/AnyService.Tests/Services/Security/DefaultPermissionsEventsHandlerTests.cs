using AnyService.Security;
using AnyService.Events;
using AnyService.Services.Security;

namespace AnyService.Tests.Services.Security
{
    public class TestClass : IEntity
    {
        public string Id { get; set; }
        public int Value { get; set; }
    }
    public class DefaultPermissionsEventsHandlerTests
    {
        #region EntityCreatedHandler
        [Fact]
        public void EntityCreatedHandler_DataIsNotDominModel_DoesNothing()
        {
            var ed = new DomainEvent
            {
                Data = "some=string",
                PerformedByUserId = "uId",
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionCreatedHandler(ed, null);
        }
        [Fact]
        public void EntityCreatedHandler_CreatesNewUserPermissions_WhenNotExistsInDatabase()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecr = new EntityConfigRecord { Type = typeof(TestClass), PermissionRecord = expPr };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new[] { ecr });

            var userId = "uId";
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId,
                WorkContext = new WorkContext { CurrentEntityConfigRecord = ecr },
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionCreatedHandler(ed, sp.Object);
            Thread.Sleep(50);

            pm.Verify(p => p.CreateUserPermissions(It.Is<UserPermissions>(
                up => up.UserId == userId
                 && up.EntityPermissions.Count() == 1
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.ReadKey)
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.UpdateKey)
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.DeleteKey))),
                  Times.Once);

        }
        [Fact]
        public void EntityCreatedHandler_AddsNewUserPermissions()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecr = new EntityConfigRecord { Type = typeof(TestClass), PermissionRecord = expPr };

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new[] { ecr });

            var userId = "uId";
            var dbUp = new UserPermissions
            {
                UserId = userId,
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(dbUp);
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId,
                WorkContext = new WorkContext { CurrentEntityConfigRecord = ecr },
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionCreatedHandler(ed, sp.Object);
            Thread.Sleep(50);

            pm.Verify(p => p.UpdateUserPermissions(It.Is<UserPermissions>(
                up => up.UserId == userId
                 && up.EntityPermissions.Count() == 1
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.ReadKey)
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.UpdateKey)
                  && up.EntityPermissions.ElementAt(0).PermissionKeys.Contains(expPr.DeleteKey))),
                  Times.Once);

        }
        [Fact]
        public void EntityCreatedHandler_UpdatesExistsUserPermissions()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecr = new EntityConfigRecord { Type = typeof(TestClass), PermissionRecord = expPr };
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new[] { ecr });

            var userId = "uId";
            var dbUp = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                     new EntityPermission
                {
                    EntityId = "eId",
                    EntityKey = "ek",
                    PermissionKeys = new[] { "r1", "u1","d1",},
                },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(dbUp);
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId,
                WorkContext = new WorkContext { CurrentEntityConfigRecord = ecr },
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionCreatedHandler(ed, sp.Object);
            Thread.Sleep(250);

            pm.Verify(p => p.UpdateUserPermissions(It.Is<UserPermissions>(
                up => up.UserId == userId
                 && up.EntityPermissions.Count() == 2
                  && up.EntityPermissions.ElementAt(1).PermissionKeys.Contains(expPr.ReadKey)
                  && up.EntityPermissions.ElementAt(1).PermissionKeys.Contains(expPr.UpdateKey)
                  && up.EntityPermissions.ElementAt(1).PermissionKeys.Contains(expPr.DeleteKey))),
                  Times.Once);

        }
        #endregion

        #region EntityDeletedHandler
        [Fact]
        public void EntityDeletedHandler_NotDomainModel()
        {
            var ed = new DomainEvent
            {
                Data = "some=string",
                PerformedByUserId = "uId"
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionDeletedHandler(ed, null);
            Thread.Sleep(50);

        }

        [Fact]
        public void EntityCreatedHandler_HasNoUserPermissions()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");

            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var userId = "uId";
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionDeletedHandler(ed, sp.Object);
            Thread.Sleep(100);

            pm.Verify(p => p.UpdateUserPermissions(It.IsAny<UserPermissions>()), Times.Never);

        }
        [Fact]
        public void EntityDeletedHandler_HasNoEntityPermissions()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var userId = "uId";
            var dbUp = new UserPermissions
            {
                UserId = userId,
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(dbUp);
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionDeletedHandler(ed, sp.Object);
            Thread.Sleep(50);

            pm.Verify(p => p.UpdateUserPermissions(It.IsAny<UserPermissions>()), Times.Never);

        }
        [Fact]
        public void EntityDeletedHandler_UpdatesExistsUserPermissions()
        {
            var eId = "eId";
            var ek = "ek";

            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecr = new EntityConfigRecord { Type = typeof(TestClass), PermissionRecord = expPr, };

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(new[] { ecr });

            var expEntityPermissions = new EntityPermission
            {
                EntityId = "eId1",
                EntityKey = "ek1",
                PermissionKeys = new[] { "r1", "u1", "d1", },
            };
            ecr = new EntityConfigRecord { EntityKey = ek, Type = typeof(TestClass), PermissionRecord = expPr };
            var userId = "uId";
            var dbUp = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                    expEntityPermissions,
                     new EntityPermission
                {
                    EntityId =eId,
                    EntityKey = ek,
                    PermissionKeys = new[] { "r", "u","d",},
                },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(dbUp);
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var data = new TestClass
            {
                Id = eId,
                Value = 123
            };

            var ed = new DomainEvent
            {
                Data = data,
                PerformedByUserId = userId,
                WorkContext = new WorkContext { CurrentEntityConfigRecord = ecr },
            };
            var ph = new DefaultPermissionsEventsHandler();
            ph.PermissionDeletedHandler(ed, sp.Object);
            Thread.Sleep(150);

            pm.Verify(p => p.UpdateUserPermissions(It.Is<UserPermissions>(
                up => up.UserId == userId
                 && up.EntityPermissions.Count() == 1
                  && up.EntityPermissions.ElementAt(0) == expEntityPermissions)),
                  Times.Once);

        }
        #endregion
    }
}