using AnyService.EntityFramework.ValueGenerators;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.EntityFramework.Tests.ValueGenerators
{
    public class GuidStringGeneratorTests
    {
        [Fact]
        public void GeneratesTemporaryValues_ReturnsFalse()
        {
            new GuidStringGenerator().GeneratesTemporaryValues.ShouldBeFalse();
        }
    }
}
