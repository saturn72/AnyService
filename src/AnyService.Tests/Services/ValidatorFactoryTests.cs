using AnyService.Services;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Services
{
    public class ValidatorFactoryTests
    {
        [Fact]
        public void ValidatorFactory_RegisterAllValidators()
        {
            var v1 = new Mock<ICrudValidator>();
            v1.SetupGet(v => v.Type).Returns(typeof(string));
            var v2 = new Mock<ICrudValidator>();
            v2.SetupGet(v => v.Type).Returns(typeof(int));

            var vf = new ValidatorFactory(new[] { v1.Object, v2.Object });
            vf[typeof(string)].ShouldBe(v1.Object);
            vf[typeof(int)].ShouldBe(v2.Object);
        }
    }

}
