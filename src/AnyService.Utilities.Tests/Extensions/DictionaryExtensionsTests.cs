using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace AnyService.Utilities.Tests.Extensions
{
    public class DictionaryExtensionsTests
    {
        [Fact]
        public void GetValueOrDefault_KeyExists()
        {
            var s = new Dictionary<int, int>()
            {
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
            };
            s.GetValueOrDefault(1).ShouldBe(1);
        }
        [Fact]
        public void GetValueOrDefault_KeyNotExists()
        {
            var s = new Dictionary<int, int>()
            {
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
            };
            s.GetValueOrDefault(40).ShouldBe(0);
        }
        [Fact]
        public void GetValueOrDefault_KeyNotExists_Override()
        {
            var s = new Dictionary<int, int>()
            {
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
            };
            s.GetValueOrDefault(40, 999).ShouldBe(999);
        }
    }
}
