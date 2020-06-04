using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace AnyService.Utilities.Tests
{
    public class GuidIdGeneratorTests
    {
        [Fact]
        public void GetNext()
        {
            var gn = new GuidIdGenerator();
            var len = 10000;
            var ids = new Guid[len];

            for (int i = 0; i < len; i++)
                ids[i] = (Guid)gn.GetNext();

            ids.Distinct().Count().ShouldBe(len);
        }
    }
}
