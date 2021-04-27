using Xunit;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace AnyService.Utilities.Tests.Extensions
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void IsNullOrEmpty_ReturnsTrue()
        {
            (null as IEnumerable<string>).IsNullOrEmpty().ShouldBeTrue();
            (new string[] { }).IsNullOrEmpty().ShouldBeTrue();
        }
        [Fact]
        public void IsNullOrEmpty_ReturnsFalse()
        {
            (new[] { 1, 2, 3 }).IsNullOrEmpty().ShouldBeFalse();
        }
        [Fact]
        public void HasMinLengthOf_ReturnsFalse_OnSmallArray()
        {
            (new[] { 1, 2, 3 }).HasMinLengthOf(4).ShouldBeFalse();
        }
        [Fact]
        public void HasMinLengthOf_ReturnsTrue()
        {
            (new[] { 1, 2, 3 }).HasMinLengthOf(2).ShouldBeTrue();
            (new[] { 1, 2, 3 }).HasMinLengthOf(3).ShouldBeTrue();
        }
        [Fact]
        public void HasMaxLengthOf_ReturnsFalse_OnSmallArray()
        {
            (new[] { 1, 2, 3 }).HasMaxLengthOf(2).ShouldBeFalse();
        }
        [Fact]
        public void HasMaxLengthOf_ReturnsTrue()
        {
            (new[] { 1, 2, 3 }).HasMaxLengthOf(4).ShouldBeTrue();
            (new[] { 1, 2, 3 }).HasMaxLengthOf(3).ShouldBeTrue();
        }
    }
}