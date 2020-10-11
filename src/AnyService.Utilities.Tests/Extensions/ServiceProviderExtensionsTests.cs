using Moq;
using Shouldly;
using System;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class ServiceProviderExtensionsTests
    {
        public class GenericService<T>
        {
            public T Value { get; set; }
        }
        [Fact]
        public void GetGenericService_ReturnsObject()
        {
            var gs = new GenericService<string>();
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(gs.GetType())).Returns(gs);

            var srv = ServiceProviderExtensions.GetGenericService(sp.Object, typeof(GenericService<>), typeof(string));
            srv.ShouldBe(gs);
        }
        [Fact]
        public void GetGenericService_ReturnsGeneric()
        {
            var gs = new GenericService<string>();
            var sp = new Mock<IServiceProvider>();

            sp.Setup(s => s.GetService(gs.GetType())).Returns(gs);

            var srv = ServiceProviderExtensions.GetGenericService<GenericService<string>>(sp.Object, typeof(GenericService<>), typeof(string));
            srv.ShouldBe(gs);
        }
    }
}
