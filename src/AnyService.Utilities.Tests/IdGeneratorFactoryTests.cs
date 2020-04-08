using AnyService.Utilities;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace AnyService.Utilities.Tests
{
    public class IdGeneratorFactoryTests
    {
        [Fact]
        public void AddOrReplace_Adds()
        {
            var f = new IdGeneratorFactory();
            f.GetGenerator(typeof(int)).ShouldBeNull();
            var g = new Mock<IIdGenerator>();
            f.AddOrReplace(typeof(int), g.Object);
            f.GetGenerator(typeof(int)).ShouldBe(g.Object);
        }

        [Fact]
        public void AddOrReplace_Replaces()
        {
            var f = new IdGeneratorFactory();

            var g1 = new Mock<IIdGenerator>();
            f.AddOrReplace(typeof(int), g1.Object);

            var g2 = new Mock<IIdGenerator>();
            f.AddOrReplace(typeof(int), g2.Object);
            f.GetGenerator(typeof(int)).ShouldBe(g2.Object);
        }
    }
}
