using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Services;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class DefaultFilterFactoryTests
    {
        public class MyClass : IDomainModelBase, IFullAudit, IPublishable
        {
            public string Id { get; set; }
            public string CreatedOnUtc { get; set; }
            public string CreatedByUserId { get; set; }
            public string CreatedWorkContextJson { get; set; }
            public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
            public bool Public { get; set; }
            public bool Deleted { get; set; }
            public string DeletedOnUtc { get; set; }
            public string DeletedByUserId { get; set; }
        }
        static readonly IEnumerable<MyClass> Table = new[]
        {
            new MyClass
            {
                CreatedByUserId = "123",
                DeletedByUserId = "123",
            },
            new MyClass
            {
                Id = "id-1",
                Public = true,
                Deleted = true,
                CreatedByUserId = "123",
                UpdateRecords = new []{
                    new UpdateRecord{
                        UpdatedByUserId = "123"
                    }
                }
            },
            new MyClass
            {
                Id = "id-2",
                CreatedByUserId = "234",
                Public = true,
            },
            new MyClass
            {
                Id = "id-3",
                CreatedByUserId = "44",
                Public = true,
            },
        };

        [Fact]
        public async Task GetAllKeys_Created()
        {
            var key = "__created";
            var wc = new WorkContext
            {
                CurrentUserId = "123",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                }
            };
            var dff = new DefaultFilterFactory(wc, null);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(2);
        }
        [Fact]
        public async Task GetAllKeys_Updated()
        {
            var key = "__updated";
            var wc = new WorkContext
            {
                CurrentUserId = "123",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                }
            };
            var dff = new DefaultFilterFactory(wc, null);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(1);
        }
        [Fact]
        public async Task GetAllKeys_Deleted()
        {
            var key = "__deleted";
            var wc = new WorkContext
            {
                CurrentUserId = "123",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                }
            };
            var dff = new DefaultFilterFactory(wc, null);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(1);
        }

        [Fact]
        public async Task GetAllKeys_CanRead()
        {
            string userId = "123",
                ek = "ek",
                pk = "pk",
                eId1 = "id-1",
                eId2 = "id-2";

            var key = "__canRead";
            var wc = new WorkContext
            {
                CurrentUserId = userId,
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                    EntityKey = ek,
                    PermissionRecord = new PermissionRecord(null, pk, null, null)
                }
            };
            var up = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                    new EntityPermission{
                        EntityId = eId1,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                    new EntityPermission{
                        EntityId = eId2,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var dff = new DefaultFilterFactory(wc, pm.Object);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(2);
        }
        [Fact]
        public async Task GetAllKeys_CanUpdate()
        {
            string userId = "123",
                ek = "ek",
                pk = "pk",
                eId1 = "id-1";

            var key = "__canUpdate";
            var wc = new WorkContext
            {
                CurrentUserId = userId,
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                    EntityKey = ek,
                    PermissionRecord = new PermissionRecord(null, null, pk, null)
                }
            };
            var up = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                    new EntityPermission{
                        EntityId = eId1,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    }
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var dff = new DefaultFilterFactory(wc, pm.Object);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(1);
        }
        [Fact]
        public async Task GetAllKeys_CanDelete()
        {
            string userId = "123",
                ek = "ek",
                pk = "pk",
                eId1 = "id-1",
                eId2 = "id-2",
                eId3 = "id-3";

            var key = "__canDelete";
            var wc = new WorkContext
            {
                CurrentUserId = userId,
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                    EntityKey = ek,
                    PermissionRecord = new PermissionRecord(null, null, null, pk)
                }
            };
            var up = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                    new EntityPermission{
                        EntityId = eId1,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                     new EntityPermission{
                        EntityId = eId2,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                     new EntityPermission{
                        EntityId = eId3,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    }
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var dff = new DefaultFilterFactory(wc, pm.Object);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(3);
        }
        [Fact]
        public async Task GetAllKeys_Public()
        {
            string userId = "123",
                ek = "ek",
                pk = "pk",
                eId1 = "id-1",
                eId2 = "id-2",
                eId3 = "id-3";

            var key = "__public";
            var wc = new WorkContext
            {
                CurrentUserId = userId,
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                    EntityKey = ek,
                    PermissionRecord = new PermissionRecord(null, null, null, pk)
                }
            };
            var up = new UserPermissions
            {
                UserId = userId,
                EntityPermissions = new[]{
                     new EntityPermission{
                        EntityId = eId1,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                    new EntityPermission{
                        EntityId = eId2,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    },
                     new EntityPermission{
                        EntityId = eId3,
                        EntityKey = "ek",
                        PermissionKeys = new []{pk}
                    }
                }
            };
            var pm = new Mock<IPermissionManager>();
            pm.Setup(p => p.GetUserPermissions(It.IsAny<string>())).ReturnsAsync(up);
            var dff = new DefaultFilterFactory(wc, pm.Object);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(2);
        }
    }
}