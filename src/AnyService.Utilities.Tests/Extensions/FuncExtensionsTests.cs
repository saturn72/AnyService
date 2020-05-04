using Shouldly;
using System;
using System.Linq.Expressions;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class FuncExtensionsTests
    {
        [Fact]
        public void ConvertFunc()
        {
            var src = new Func<object, int>(x => ((int)x) * 2);
            var dest = FuncExtensions.Convert<object, int, int>(src);
            var f = dest.ShouldBeOfType<Func<int, int>>();
            f(2).ShouldBe(4);
        }
        [Fact]
        public void ConvertExpression()
        {
            Func<object, int> src = x => ((int)x) * 2;
            Expression<Func<object, int>> srcExp = y => src(y);

            var dest = FuncExtensions.Convert<object, int, int>(srcExp);
            var f = dest.Compile();
            f(2).ShouldBe(4);
        }
    }
}
