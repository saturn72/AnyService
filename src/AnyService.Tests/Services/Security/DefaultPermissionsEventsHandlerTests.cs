using System;
using System.Linq;
using System.Threading;
using AnyService.Security;
using AnyService.Events;
using AnyService.Services.Security;
using Moq;
using Xunit;

namespace AnyService.Tests.Services.Security
{
    public class TestClass : IDomainModelBase
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
            var ed = new DomainEventData
            {
                Data = "some=string",
                PerformedByUserId = "uId"
            };
            var ph = new DefaultPermissionsEventsHandler(null);
            ph.EntityCreatedHandler(ed);
        }
        [Fact]
        public void EntityCreatedHandler_CreatesNewUserPermissions_WhenNotExistsInDatabase()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = new[] {
             new EntityConfigRecord {
                 Type = typeof(TestClass),
                 PermissionRecord = expPr
                 }
                 }
            }
                 ;
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(null as UserPermissions);
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            sp.Setup(s => s.GetService(typeof(EntityConfigRecordManager))).Returns(ecrm);
            AppEngine.Init(sp.Object);
            var userId = "uId";
            var data = new TestClass
            {
                Value = 123
            };

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityCreatedHandler(ed);
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
            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = new[] {
                    new EntityConfigRecord {
                        Type = typeof(TestClass),
                        PermissionRecord = expPr
                    }
                }
            };

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(EntityConfigRecordManager))).Returns(ecrm);
            AppEngine.Init(sp.Object);
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

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityCreatedHandler(ed);
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
            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = new[]
            {
             new EntityConfigRecord {
                 Type = typeof(TestClass),
                 PermissionRecord = expPr
                 }
                 }
            }
                 ;
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(EntityConfigRecordManager))).Returns(ecrm);
            AppEngine.Init(sp.Object);

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

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityCreatedHandler(ed);
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
            var ed = new DomainEventData
            {
                Data = "some=string",
                PerformedByUserId = "uId"
            };
            var ph = new DefaultPermissionsEventsHandler(null);
            ph.EntityDeletedHandler(ed);
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

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityDeletedHandler(ed);
            Thread.Sleep(100);

            pm.Verify(p => p.UpdateUserPermissions(It.IsAny<UserPermissions>()), Times.Never);

        }
        [Fact]
        public void EntityDeletedHandler_HasNoEntityPermissions()
        {
            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = new[] {
             new EntityConfigRecord {
                 Type = typeof(TestClass),
                 PermissionRecord = expPr
                 }
                 }
            };
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

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityDeletedHandler(ed);
            Thread.Sleep(50);

            pm.Verify(p => p.UpdateUserPermissions(It.IsAny<UserPermissions>()), Times.Never);

        }
        [Fact]
        public void EntityDeletedHandler_UpdatesExistsUserPermissions()
        {
            var eId = "eId";
            var ek = "ek";

            var expPr = new PermissionRecord("c", "r", "u", "d");
            var ecrm = new EntityConfigRecordManager
            {
                EntityConfigRecords = new[] {
             new EntityConfigRecord {
                 Type = typeof(TestClass),
                 PermissionRecord = expPr,
                 }
                 }
            };

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(EntityConfigRecordManager))).Returns(ecrm);
            AppEngine.Init(sp.Object);

            var expEntityPermissions = new EntityPermission
            {
                EntityId = "eId1",
                EntityKey = "ek1",
                PermissionKeys = new[] { "r1", "u1", "d1", },
            };
            ecrm.EntityConfigRecords = new[] {
             new EntityConfigRecord {
                 EntityKey = ek,
                 Type = typeof(TestClass),
                 PermissionRecord = expPr
                 }
                 }
                 ;
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

            var ed = new DomainEventData
            {
                Data = data,
                PerformedByUserId = userId
            };
            var ph = new DefaultPermissionsEventsHandler(sp.Object);
            ph.EntityDeletedHandler(ed);
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