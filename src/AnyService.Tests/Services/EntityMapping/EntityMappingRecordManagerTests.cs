using AnyService.Events;
using AnyService.Security;
using AnyService.Services;
using AnyService.Services.Audit;
using AnyService.Services.EntityMapping;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Tests.Services.EntityMapping
{
    public class EntityMappingRecordManagerTests
    {
        #region UpdateMapping
        [Theory]
        [MemberData(nameof(UpdateMapping_MissingEntityConfigRecord_ReturnsBadRequest_DATA))]
        public async Task UpdateMapping_MissingEntityConfigRecord_ReturnsBadRequest(EntityMappingRequest request)
        {
            var sp = new Mock<IServiceProvider>();
            var ecrs = new[]
            {
                new EntityConfigRecord{EntityKey = "exists", EntityMappingSettings = new EntityMappingSettings()},
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);
            var log = new Mock<ILogger<EntityMappingRecordManager>>();
            var mgr = new EntityMappingRecordManager(sp.Object, log.Object);
            var res = await mgr.UpdateMapping(request);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        public static IEnumerable<object[]> UpdateMapping_MissingEntityConfigRecord_ReturnsBadRequest_DATA => new[]
        {
            new object[]{ new EntityMappingRequest { ParentEntityKey = "not-exists", ChildEntityKey = "exists"} },
            new object[]{ new EntityMappingRequest { ParentEntityKey = "exists", ChildEntityKey = "not-exists" } }
        };

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task UpdateMapping_DisabledForMapping_ReturnsBadRequest(bool disabledAsParent, bool disabledAsChild)
        {
            var sp = new Mock<IServiceProvider>();
            var ecrs = new[]
            {
                new EntityConfigRecord{EntityKey = "e1", EntityMappingSettings = new EntityMappingSettings{ DisabledAsParent = disabledAsParent}, },
                new EntityConfigRecord{EntityKey = "e2", EntityMappingSettings = new EntityMappingSettings{ DisabledAsChild = disabledAsChild }, },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);
            var log = new Mock<ILogger<EntityMappingRecordManager>>();
            var mgr = new EntityMappingRecordManager(sp.Object, log.Object);
            var request = new EntityMappingRequest { ParentEntityKey = "e1", ChildEntityKey = "e2" };
            var res = await mgr.UpdateMapping(request);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task UpdateMapping_NoUpdatePermissionsOnParent_ReturnsBadRequest()
        {
            string parentKey = "e1",
                childKey = "e2",
                parentId = "p-id",
                updatePermissionKey = "update";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var sp = new Mock<IServiceProvider>();
            var ecrs = new[]
           {
                new EntityConfigRecord
                {
                    EntityKey = parentKey,
                    EntityMappingSettings = new EntityMappingSettings(),
                    PermissionRecord = new PermissionRecord("c", "r", updatePermissionKey, "d"),
                    EventKeys = ekr,
                },
                new EntityConfigRecord{ EntityKey = childKey, EntityMappingSettings = new EntityMappingSettings(), PermissionRecord = new PermissionRecord("c", "r", updatePermissionKey, "d")},
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);
            var wc = new WorkContext { CurrentUserId = "uId" };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(new UserPermissions { });
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var log = new Mock<ILogger<EntityMappingRecordManager>>();
            var mgr = new EntityMappingRecordManager(sp.Object, log.Object);
            var request = new EntityMappingRequest { ParentEntityKey = parentKey, ParentId = parentId, ChildEntityKey = childKey, Add = new[] { "a" } };
            var res = await mgr.UpdateMapping(request);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task UpdateMapping_NoUpdatePermissionsOnChilds_ReturnsBadRequest()
        {
            string parentKey = "e1",
                childKey = "e2",
                parentId = "p-id",
                updatePermissionKey = "update";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var sp = new Mock<IServiceProvider>();

            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    EntityKey = parentKey,
                    EntityMappingSettings = new EntityMappingSettings(),
                    PermissionRecord = new PermissionRecord("c", "r", updatePermissionKey, "d"),
                    EventKeys = ekr,
                },
                new EntityConfigRecord{ EntityKey = childKey, EntityMappingSettings = new EntityMappingSettings(), PermissionRecord = new PermissionRecord("c", "r", updatePermissionKey, "d")},
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);
            var wc = new WorkContext { CurrentUserId = "uId" };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var pm = new Mock<IPermissionManager>();
            var up = new UserPermissions
            {
                UserId = wc.CurrentUserId,
                EntityPermissions = new[]
                {
                    new EntityPermission
                    {
                        EntityKey = parentKey, EntityId = parentId, PermissionKeys = new[] { updatePermissionKey }
                    },
                    new EntityPermission
                    {
                        EntityKey = parentKey, EntityId = parentId, PermissionKeys = new[] { "c" }
                    },
                },
            };
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(null as IEventBus);
            var log = new Mock<ILogger<EntityMappingRecordManager>>();
            var mgr = new EntityMappingRecordManager(sp.Object, log.Object);
            var request = new EntityMappingRequest { ParentEntityKey = "e1", ParentId = parentId, ChildEntityKey = "e2", Add = new[] { "a" } };
            var res = await mgr.UpdateMapping(request);
            res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        }
        [Fact]
        public async Task UpdateMapping_UpdateSuccess()
        {
            string parentKey = "e1",
                 childKey = "e2",
                 parentId = "p-id",
                 childIdToAdd = "to-add",
                 childIdToRemove = "to-remove",
                 updatePermissionKey = "update";
            var ekr = new EventKeyRecord("create", "read", "update", "delete");
            var sp = new Mock<IServiceProvider>();

            var ecrs = new[]
            {
                new EntityConfigRecord
                {
                    EntityKey = parentKey,
                    EntityMappingSettings = new EntityMappingSettings(),
                    PermissionRecord = new PermissionRecord(null, null, updatePermissionKey, null),
                    EventKeys = ekr,
                },
                new EntityConfigRecord
                {
                    EntityKey = childKey,
                    EntityMappingSettings = new EntityMappingSettings(),
                    PermissionRecord =
                    new PermissionRecord(null, null, updatePermissionKey, null)
                },
            };
            sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);
            var wc = new WorkContext { CurrentUserId = "uId" };
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
            var pm = new Mock<IPermissionManager>();
            var up = new UserPermissions
            {
                UserId = wc.CurrentUserId,
                EntityPermissions = new[]
                {
                    new EntityPermission
                    {
                        EntityKey = parentKey, EntityId = parentId, PermissionKeys = new[] { updatePermissionKey }
                    },
                    new EntityPermission
                    {
                        EntityKey = childKey, EntityId = childIdToAdd, PermissionKeys = new[] { updatePermissionKey }
                    },
                    new EntityPermission
                    {
                        EntityKey = childKey, EntityId = childIdToRemove, PermissionKeys = new[] { updatePermissionKey }
                    },
                },
            };
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            sp.Setup(s => s.GetService(typeof(IPermissionManager))).Returns(pm.Object);
            var eb = new Mock<IEventBus>();
            sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
            var repo = new Mock<IRepository<EntityMappingRecord>>();
            sp.Setup(s => s.GetService(typeof(IRepository<EntityMappingRecord>))).Returns(repo.Object);
            var log = new Mock<ILogger<EntityMappingRecordManager>>();
            var mgr = new EntityMappingRecordManager(sp.Object, log.Object);
            var request = new EntityMappingRequest
            {
                ParentEntityKey = parentKey,
                ParentId = parentId,
                ChildEntityKey = childKey,
                Add = new[] { childIdToAdd },
                Remove = new[] { childIdToRemove }
            };
            var res = await mgr.UpdateMapping(request);
            res.Result.ShouldBe(ServiceResult.Ok);

            repo.Verify(r => r.BulkDelete(It.Is<IEnumerable<EntityMappingRecord>>(e => e.ElementAt(0).ChildId == childIdToRemove), It.IsAny<bool>()), Times.Once);
            repo.Verify(r => r.BulkInsert(It.Is<IEnumerable<EntityMappingRecord>>(e => e.ElementAt(0).ChildId == childIdToAdd), It.IsAny<bool>()), Times.Once);
        }

        //    //[Theory]
        //    //[InlineData("ch-id1, ch-id2", null)]
        //    //[InlineData("ch-id1, ch-id2", "ch-id3, ch-id4")]
        //    //[InlineData("ch-id1, ch-id2", "ch-id2, ch-id3, ch-id4")]
        //    //[InlineData("ch-id1, ch-id2", null)]

        //    [Fact]
        //    public async Task UpdateMappings_NullRequest_ReturnsBadOrMissingData()
        //    {
        //        var ekr = new EventKeyRecord("create", "read", "update", "delete");
        //        var wc = new WorkContext
        //        {
        //            CurrentUserId = "some-user-id",
        //            CurrentEndpointSettings = new EndpointSettings
        //            {
        //                EntityConfigRecord = new EntityConfigRecord
        //                {
        //                    Type = typeof(AggregateRootEntity),
        //                    EntityKey = "AggregateRootEntity",
        //                    EventKeys = ekr,
        //                    PaginationSettings = new PaginationSettings(),
        //                }
        //            },
        //        };
        //        var sp = new Mock<IServiceProvider>();

        //        sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
        //        var eb = new Mock<IEventBus>();
        //        sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
        //        sp.Setup(s => s.GetService(typeof(IAuditManager)));
        //        var ecrs = new[]
        //      {
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregateRootEntity),
        //                EntityKey = "AggregateRootEntity",
        //                EventKeys = ekr,
        //                PaginationSettings = new PaginationSettings(),
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(OptionEntity),
        //                EntityKey = "OptionEntity",
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregatedChild),
        //                EntityKey = "Aggregated",
        //            },
        //        };
        //        sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

        //        var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
        //        var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
        //        var res = await cSrv.UpdateMappings(null);

        //        res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        //    }

        //    [Fact]
        //    public async Task UpdateMappings_NamedChildTypeNotConfigured()
        //    {
        //        var parentId = "p-id";
        //        var ekr = new EventKeyRecord("create", "read", "update", "delete");
        //        var wc = new WorkContext
        //        {
        //            CurrentUserId = "some-user-id",
        //            CurrentEndpointSettings = new EndpointSettings
        //            {
        //                EntityConfigRecord = new EntityConfigRecord
        //                {
        //                    Type = typeof(AggregateRootEntity),
        //                    EntityKey = "AggregateRootEntity",
        //                    EventKeys = ekr,
        //                    PaginationSettings = new PaginationSettings(),
        //                },
        //            }
        //        };
        //        var sp = new Mock<IServiceProvider>();

        //        sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
        //        var eb = new Mock<IEventBus>();
        //        sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
        //        sp.Setup(s => s.GetService(typeof(IAuditManager)));
        //        var ecrs = new[]
        //      {
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregateRootEntity),
        //                EntityKey = "AggregateRootEntity",
        //                EventKeys = ekr,
        //                PaginationSettings = new PaginationSettings(),
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(OptionEntity),
        //                EntityKey = "OptionEntity",
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregatedChild),
        //                EntityKey = "Aggregated",
        //            },
        //        };
        //        sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

        //        var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
        //        var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
        //        var req = new EntityMappingRequest
        //        {
        //            ParentId = parentId,
        //            ChildIdsToAdd = new[] { "a", "b", "c" },
        //            ChildIdsToRemove = new[] { "d", "e" },
        //            ChildEntityKey = "not-exists",
        //        };
        //        var res = await cSrv.UpdateMappings(req);

        //        res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        //    }
        //    [Fact]
        //    public async Task UpdateMappings_NotAllChildEntitiesExists_ReturnsBadResult()
        //    {
        //        var parentId = "p-id";
        //        var ekr = new EventKeyRecord("create", "read", "update", "delete");
        //        var wc = new WorkContext
        //        {
        //            CurrentUserId = "some-user-id",
        //            CurrentEndpointSettings = new EndpointSettings
        //            {
        //                EntityConfigRecord = new EntityConfigRecord
        //                {
        //                    Type = typeof(AggregateRootEntity),
        //                    EntityKey = "AggregateRootEntity",
        //                    EventKeys = ekr,
        //                    PaginationSettings = new PaginationSettings(),
        //                }
        //            },
        //        };
        //        var sp = new Mock<IServiceProvider>();

        //        sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
        //        var eb = new Mock<IEventBus>();
        //        sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
        //        sp.Setup(s => s.GetService(typeof(IAuditManager)));
        //        var ecrs = new[]
        //      {
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregateRootEntity),
        //                EntityKey =  "AggregateRootEntity",
        //                EventKeys = ekr,
        //                PaginationSettings = new PaginationSettings(),
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(OptionEntity),
        //                EntityKey =  "OptionEntity",
        //            },
        //             new EntityConfigRecord
        //            {
        //                Type = typeof(AggregatedChild),
        //                EntityKey =  typeof(AggregatedChild).Name,
        //            },
        //        };
        //        sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

        //        var addChData = new[]
        //        {
        //            new AggregatedChild{Id = "a"}
        //        };
        //        var aggChRepo = new Mock<IRepository<AggregatedChild>>();
        //        aggChRepo.Setup(ar => ar.Collection).ReturnsAsync(addChData.AsQueryable());
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggChRepo.Object);


        //        var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
        //        var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
        //        var req = new EntityMappingRequest
        //        {
        //            ParentId = parentId,
        //            ChildIdsToAdd = new[] { "a", "b", "c" },
        //            ChildIdsToRemove = new[] { "d", "e" },
        //            ChildEntityKey = typeof(AggregatedChild).Name,
        //        };
        //        var res = await cSrv.UpdateMappings(req);

        //        res.Result.ShouldBe(ServiceResult.BadOrMissingData);
        //    }
        //    [Fact]
        //    public async Task UpdateMappings_Maps()
        //    {
        //        var parentId = "p-id";
        //        var ekr = new EventKeyRecord("created", "read", null, "deleted");
        //        var wc = new WorkContext
        //        {
        //            CurrentUserId = "some-user-id",
        //            CurrentEndpointSettings = new EndpointSettings
        //            {
        //                EntityConfigRecord = new EntityConfigRecord
        //                {
        //                    Type = typeof(AggregateRootEntity),
        //                    EntityKey = "AggregateRootEntity",
        //                    EventKeys = ekr,
        //                    PaginationSettings = new PaginationSettings(),
        //                }
        //            },
        //        };
        //        var sp = new Mock<IServiceProvider>();

        //        sp.Setup(s => s.GetService(typeof(AnyServiceConfig)));
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(CrudValidatorBase<AggregateRootEntity>)));
        //        sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);
        //        var eb = new Mock<IEventBus>();
        //        sp.Setup(s => s.GetService(typeof(IEventBus))).Returns(eb.Object);
        //        sp.Setup(s => s.GetService(typeof(IAuditManager)));
        //        var ecrs = new[]
        //        {
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(AggregateRootEntity),
        //                EntityKey =  typeof(AggregateRootEntity).Name,
        //                EventKeys = ekr,
        //                PaginationSettings = new PaginationSettings(),
        //            },
        //            new EntityConfigRecord
        //            {
        //                Type = typeof(OptionEntity),
        //                EntityKey =  "OptionEntity",
        //            },
        //              new EntityConfigRecord
        //            {
        //                Type = typeof(AggregatedChild),
        //                EntityKey =  "AggregatedChild",
        //            },
        //        };
        //        sp.Setup(s => s.GetService(typeof(IEnumerable<EntityConfigRecord>))).Returns(ecrs);

        //        var mapRepo = new Mock<IRepository<EntityMappingRecord>>();
        //        var mapData = new[]
        //        {
        //            new EntityMapping
        //            {
        //                ParentId = parentId,
        //                ParentEntityEntityKey =  typeof(AggregateRootEntity).Name,
        //                ChildId = "d",
        //                ChildEntityEntityKey =  typeof(AggregatedChild).Name
        //            }
        //        };
        //        mapRepo.Setup(mr => mr.Collection).ReturnsAsync(mapData.AsQueryable());
        //        sp.Setup(s => s.GetService(typeof(IRepository<EntityMapping>))).Returns(mapRepo.Object);
        //        var addChData = new[]
        //{
        //            new AggregatedChild { Id = "a" },
        //            new AggregatedChild { Id = "b" },
        //            new AggregatedChild { Id = "c" },
        //        };
        //        var aggChRepo = new Mock<IRepository<AggregatedChild>>();
        //        aggChRepo.Setup(ar => ar.Collection).ReturnsAsync(addChData.AsQueryable());
        //        sp.Setup(s => s.GetService(typeof(IRepository<AggregatedChild>))).Returns(aggChRepo.Object);

        //        var logger = new Mock<ILogger<CrudService<AggregateRootEntity>>>();
        //        var cSrv = new CrudService<AggregateRootEntity>(sp.Object, logger.Object);
        //        var expIds = new[] { "a", "b", "c" };
        //        var req = new EntityMappingRequest
        //        {
        //            ParentId = parentId,
        //            ChildIdsToAdd = expIds,
        //            ChildIdsToRemove = new[] { "d", "e" },
        //            ChildEntityKey = typeof(AggregatedChild).Name,
        //        };

        //        var res = await cSrv.UpdateMappings(req);
        //        res.Result.ShouldBe(ServiceResult.Ok);

        //        mapRepo.Verify(mr => mr.BulkDelete(It.Is<IEnumerable<EntityMapping>>(en => en.Count() == 1 && en.First().ChildId == "d"), It.IsAny<bool>()), Times.Once);
        //        Func<IEnumerable<EntityMapping>, bool> VerifyBulkInsertEntities =
        //            entities =>
        //            {
        //                var cEntityKey = typeof(AggregatedChild).Name;
        //                var pEntityKey = typeof(AggregateRootEntity).Name;
        //                return entities.Count() == 3 &&
        //                entities.All(e =>
        //                    e.ParentEntityEntityKey = = pName &&
        //                    e.ParentId == parentId &&
        //                    e.ChildEntityEntityKey = = cName &&
        //                    expIds.Contains(e.ChildId));
        //            };
        //        mapRepo.Verify(mr => mr.BulkInsert(It.Is<IEnumerable<EntityMapping>>(e => VerifyBulkInsertEntities(e)), It.IsAny<bool>()), Times.Once);
        //        eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Delete), It.IsAny<DomainEventData>()), Times.Once);
        //        eb.Verify(e => e.Publish(It.Is<string>(s => s == ekr.Create), It.IsAny<DomainEventData>()), Times.Once);
        //    }
        #endregion

    }
}
