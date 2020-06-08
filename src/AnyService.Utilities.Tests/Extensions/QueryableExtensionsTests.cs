using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class QueryableExtensionsTests
    {

        [Fact]
        public void OrderByDynamic()
        {
            var l = new List<MyTestClass>
            {
                new MyTestClass
                {
                    StringValue = "a"
                },
                new MyTestClass
                {
                    StringValue = "c"
                },
                new MyTestClass
                {
                    StringValue = "d"
                },
                new MyTestClass
                {
                    StringValue = "a"

                },
                }.AsQueryable();

            var q1 = l.OrderBy(nameof(MyTestClass.StringValue), false);
            var a1 = q1.ToArray();
            a1[0].StringValue.ShouldBe("a");
            a1[1].StringValue.ShouldBe("a");
            a1[2].StringValue.ShouldBe("c");
            a1[3].StringValue.ShouldBe("d");

            var q2 = l.OrderBy(nameof(MyTestClass.StringValue), true);
            var a2 = q2.ToArray();
            a2[0].StringValue.ShouldBe("d");
            a2[1].StringValue.ShouldBe("c");
            a2[2].StringValue.ShouldBe("a");
            a2[3].StringValue.ShouldBe("a");
        }
    }
}
