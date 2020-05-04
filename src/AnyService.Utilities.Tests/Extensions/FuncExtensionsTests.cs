using Shouldly;
using System;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class FuncExtensionsTests
    {
        [Fact]
        public void Convert()
        {
            var src = new Func<object, int>(x => ((int)x) * 2);
            var dest = FuncExtensions.Convert<object, int, int>(src);
            var f = dest.ShouldBeOfType<Func<int, int>>();
            f(2).ShouldBe(4);
        }
    }
}
