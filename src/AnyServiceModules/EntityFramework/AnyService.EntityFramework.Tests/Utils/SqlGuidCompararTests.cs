using AnyService.EntityFramework.Utils;
using Shouldly;
using System.Data.SqlTypes;
using Xunit;

namespace AnyService.EntityFramework.Tests.Utils
{
    public class SqlGuidCompararTests
    {
        [Theory]
        [InlineData("ED1AAC52-2BEA-45A5-51B4-08D85F9BB4EA", "408E4159-455D-4B7C-51B5-08D85F9BB4EA", -1)]
        [InlineData("408E4159-455D-4B7C-51B5-08D85F9BB4EA", "408E4159-455D-4B7C-51B5-08D85F9BB4EA", 0)]
        [InlineData("408E4159-455D-4B7C-51B5-08D85F9BB4EA", "ED1AAC52-2BEA-45A5-51B4-08D85F9BB4EA", 1)]
        public void SqlGuidFieldComperar_Strings(string g1, string g2, int exp)
        {
            SqlGuidComparar.CompareTo(g1, g2).ShouldBe(exp);
        }
        [Theory]
        [InlineData("ED1AAC52-2BEA-45A5-51B4-08D85F9BB4EA", "408E4159-455D-4B7C-51B5-08D85F9BB4EA", -1)]
        [InlineData("408E4159-455D-4B7C-51B5-08D85F9BB4EA", "408E4159-455D-4B7C-51B5-08D85F9BB4EA", 0)]
        [InlineData("408E4159-455D-4B7C-51B5-08D85F9BB4EA", "ED1AAC52-2BEA-45A5-51B4-08D85F9BB4EA", 1)]
        public void SqlGuidFieldComperar_SqlGuids(string g1, string g2, int exp)
        {
            SqlGuidComparar.CompareTo(new SqlGuid(g1), new SqlGuid(g2)).ShouldBe(exp);
        }
    }
}
