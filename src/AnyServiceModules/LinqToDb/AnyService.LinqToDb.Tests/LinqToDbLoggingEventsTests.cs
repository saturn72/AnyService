using Shouldly;
using Xunit;

namespace AnyService.LinqToDb.Tests
{
    public class LinqToDbLoggingEventsTests
    {
        [Fact]
        public void Test1()
        {
            LinqToDbLoggingEvents.BulkInsert.Id.ShouldBe(1);
            LinqToDbLoggingEvents.BulkInsert.Name.ShouldBe("bulk-insert");
            LinqToDbLoggingEvents.Insert.Id.ShouldBe(2);
            LinqToDbLoggingEvents.Insert.Name.ShouldBe("insert");
            LinqToDbLoggingEvents.BulkRead.Id.ShouldBe(3);
            LinqToDbLoggingEvents.BulkRead.Name.ShouldBe("bulk-read");
            LinqToDbLoggingEvents.Read.Id.ShouldBe(4);
            LinqToDbLoggingEvents.Read.Name.ShouldBe("read");
            LinqToDbLoggingEvents.BulkUpdate.Id.ShouldBe(5);
            LinqToDbLoggingEvents.BulkUpdate.Name.ShouldBe("bulk-update");
            LinqToDbLoggingEvents.Update.Id.ShouldBe(6);
            LinqToDbLoggingEvents.Update.Name.ShouldBe("update");
            LinqToDbLoggingEvents.BulkDelete.Id.ShouldBe(7);
            LinqToDbLoggingEvents.BulkDelete.Name.ShouldBe("bulk-delete");
            LinqToDbLoggingEvents.Delete.Id.ShouldBe(8);
            LinqToDbLoggingEvents.Delete.Name.ShouldBe("delete");
        }
    }
}
