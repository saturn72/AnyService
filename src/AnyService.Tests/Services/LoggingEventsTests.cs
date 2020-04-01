using AnyService.Services;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class LoggingEventsTests
    {
        [Fact]
        public void Allfields()
        {
            LoggingEvents.BusinessLogicFlow.Id.ShouldBe(1);
            LoggingEvents.BusinessLogicFlow.Name.ShouldBe("business-logic-flow");
            LoggingEvents.Audity.Id.ShouldBe(2);
            LoggingEvents.Audity.Name.ShouldBe("audity");
            LoggingEvents.Repository.Id.ShouldBe(3);
            LoggingEvents.Repository.Name.ShouldBe("repository");
            LoggingEvents.EventPublishing.Id.ShouldBe(4);
            LoggingEvents.EventPublishing.Name.ShouldBe("event-publishing");
            LoggingEvents.Validation.Id.ShouldBe(5);
            LoggingEvents.Validation.Name.ShouldBe("validation");
            LoggingEvents.Controller.Id.ShouldBe(6);
            LoggingEvents.Controller.Name.ShouldBe("controller");
            LoggingEvents.Permission.Id.ShouldBe(7);
            LoggingEvents.Permission.Name.ShouldBe("permission");
            LoggingEvents.WorkContext.Id.ShouldBe(7);
            LoggingEvents.WorkContext.Name.ShouldBe("workcontext");
            LoggingEvents.Authorization.Id.ShouldBe(8);
            LoggingEvents.Authorization.Name.ShouldBe("authorization");
        }
    }
}