using Shouldly;
using AnyService.Services;
using Xunit;

namespace AnyService.Tests.Services
{
    public sealed class ServiceResponseWrapperTests
    {
        [Fact]
        public void ctor()
        {
            var sr = new ServiceResponse();
            var w = new ServiceResponseWrapper(sr);
            w.ServiceResponse.ShouldBe(sr);
            w.Exception.ShouldBeNull();
        }
    }
}