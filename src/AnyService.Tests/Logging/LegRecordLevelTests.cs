using AnyService.Logging;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Logging
{
    public class LegRecordLevelTests
    {
        [Fact]
        public void All_Levels()
        {
            LogRecordLevel.Debug.ShouldBe("DEBUG");
            LogRecordLevel.Information.ShouldBe("INFO");
            LogRecordLevel.Warning.ShouldBe("WARNING");
            LogRecordLevel.Error.ShouldBe("ERROR");
            LogRecordLevel.Fatal.ShouldBe("FATAL");
        }
    }
}
