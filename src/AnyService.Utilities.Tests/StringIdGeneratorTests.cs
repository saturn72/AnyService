using System.Linq;
using Shouldly;
using Xunit;

namespace AnyService.Utilities.Tests
{
    public class StringIdGeneratorTests
    {
        [Fact]
        public void GetNext()
        {
            var gn = new StringIdGenerator();
            var len = 10000;
            var ids = new string[len];

            for (int i = 0; i < len; i++)
                ids[i] = gn.GetNext() as string;

            ids.Distinct().Count().ShouldBe(len);
        }
    }
}
