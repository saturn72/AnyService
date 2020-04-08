using System;
using Moq;
using Xunit;

namespace AnyService.Utilities.Tests
{
    public class IdGeneratorFactoryExtensionsTests
    {
        [Fact]
        public void GetGenerator()
        {
            var f = new Mock<IdGeneratorFactory>();
            IdGeneratorFactoryExtensions.GetGenerator<string>(f.Object);
            f.Verify(fc => fc.GetGenerator(It.Is<Type>(t => t == typeof(string))), Times.Once);
        }

        [Fact]
        public void GetNext()
        {
            var g = new Mock<IIdGenerator>();
            var f = new IdGeneratorFactory();
            f.AddOrReplace(typeof(string), g.Object);
            f.GetNext<string>();
            g.Verify(gn => gn.GetNext(), Times.Once);
        }
    }
}
