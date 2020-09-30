using Shouldly;
using System.Linq;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class LinqExtensionsTests
    {
        public class MyClass
        {
            public int I { get; set; }
        }
        [Fact]
        public void DistinctBy_RemovesDuplications()
        {
            var src = new[]
            {
                new MyClass{I = 1},
                new MyClass{I = 2},
                new MyClass{I = 1},
            };
            var r = src.DistinctBy(x => x.I);
            r.Count().ShouldBe(2);
            r.ShouldContain(x => x.I == 1);
            r.ShouldContain(x => x.I == 2);
        }
        [Fact]
        public void DistinctBy_WhenNotRequired()
        {
            var src = new[]
            {
                new MyClass{I = 1},
                new MyClass{I = 2},
            };
            var r = src.DistinctBy(x => x.I);
            r.Count().ShouldBe(2);
            r.ShouldContain(x => x.I == 1);
            r.ShouldContain(x => x.I == 2);
        }
    }
}
