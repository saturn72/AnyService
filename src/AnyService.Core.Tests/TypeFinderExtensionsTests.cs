using Moq;
using System;
using Xunit;

namespace AnyService.Core.Tests
{
    public class TypeFinderExtensionsTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TypeFinderExtensions_FindObjectsOfType_Generic(bool concretesOnly)
        {
            var finder = new Mock<ITypeFinder>();

            TypeFinderExtensions.FindAllTypesOf<string>(finder.Object, concretesOnly);
            finder.Verify(f => f.GetAllTypesOf(It.Is<Type>(t => t == typeof(string)), It.Is<bool>(b => b == concretesOnly)), Times.Once);
        }
    }
}
