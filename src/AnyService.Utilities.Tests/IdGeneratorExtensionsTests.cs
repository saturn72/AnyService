using Moq;
using Xunit;

namespace AnyService.Utilities.Tests
{
    public class IdGeneratorExtensionsTests
    {
        [Fact]
        public void GetNext()
        {
            var g = new Mock<IIdGenerator>();
            IdGeneratorExtensions.GetNext<string>(g.Object);
            g.Verify(gn => gn.GetNext(), Times.Once);
        }
    }
}
