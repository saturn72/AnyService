using AnyService.Services;

namespace AnyService.Tests.Services
{
    public sealed class ServiceResultTests
    {
        [Fact]
        public void ServiceResultAllMembers()
        {
            ServiceResult.All.Count().ShouldBe(7);

            ServiceResult.Accepted.ShouldBe("accepted");
            ServiceResult.BadOrMissingData.ShouldBe("bad-or-missing-data");
            ServiceResult.Error.ShouldBe("error");
            ServiceResult.NotFound.ShouldBe("not-found");
            ServiceResult.NotSet.ShouldBe("not-set");
            ServiceResult.Ok.ShouldBe("ok");
            ServiceResult.Unauthorized.ShouldBe("unauthorized");
        }
    }
}