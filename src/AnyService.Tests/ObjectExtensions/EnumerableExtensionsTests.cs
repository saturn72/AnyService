using System.Collections;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace AnyService.Tests.ObjectExtensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void ForEachItem_IterateNonGeneric()
        {
            var actualRes = 0;
            var collection = new ArrayList() { 1, 2, 3, 4, 5 };
            collection.ForEachItem(i => actualRes += (int)i);
            actualRes.ShouldBe(1 + 2 + 3 + 4 + 5);

        }
        [Fact]
        public void ForEachItem_IterateGeneric()
        {
            var collection = new List<int> { 1, 2, 3, 4, 5 };
            var actualRes = 0;
            (collection as IEnumerable<int>).ForEachItem(i => actualRes += i);
            actualRes.ShouldBe(1 + 2 + 3 + 4 + 5);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsNullOrEmptyReturnTrueNonGeneric(IEnumerable collection)
        {
            collection.IsNullOrEmpty().ShouldBeTrue();
        }

        [Fact]
        public void IsNullOrEmptyReturnFalseNonGeneric()
        {
            "ddd".IsNullOrEmpty().ShouldBeFalse();
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsNullOrEmptyReturnTrue_Generic(IEnumerable<char> collection)
        {
            collection.IsNullOrEmpty().ShouldBeTrue();
        }

        [Fact]
        public void IsNullOrEmptyReturnFalse_Generic()
        {
            "dddd".IsNullOrEmpty().ShouldBeFalse();
        }

    }
}