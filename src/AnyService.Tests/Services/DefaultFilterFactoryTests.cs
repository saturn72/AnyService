using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Core.Security;
using AnyService.Services;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class DefaultFilterFactoryTests
    {
        public class MyClass : IDomainModelBase, IFullAudit
        {
            public string Id { get; set; }
            public string CreatedOnUtc { get; set; }
            public string CreatedByUserId { get; set; }
            public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
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
                CreatedByUserId = "123",
                UpdateRecords = new []{
                    new UpdateRecord{
                        UpdatedByUserId = "123"
                    }
                }
            },
            new MyClass
            {
                CreatedByUserId = "234",
            },
            new MyClass
            {
                CreatedByUserId = "44",
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
            var key = "__canRead";
            var wc = new WorkContext
            {
                CurrentUserId = "123",
                CurrentEntityConfigRecord = new EntityConfigRecord
                {
                    Type = typeof(MyClass),
                }
            };
            var pm = new Mock<IPermissionManager>();
            var dff = new DefaultFilterFactory(wc, pm.Object);
            var d = await dff.GetFilter<MyClass>(key);
            var f = d("dd");
            var res = Table.Where(f);
            res.Count().ShouldBe(4);
        }
    }
}