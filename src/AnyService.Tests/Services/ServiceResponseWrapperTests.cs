using Shouldly;
using AnyService.Services;
using Xunit;

namespace AnyService.Tests.Services
{
    public sealed class ServiceResponseWrapperTests
    {
        [Fact]
        public void Ctor()
        {
            var sr = new ServiceResponse<object>();
            var w = new ServiceResponseWrapper(sr);
            w.ServiceResponse.ShouldBe(sr);
            w.Exception.ShouldBeNull();
        }
    }
}