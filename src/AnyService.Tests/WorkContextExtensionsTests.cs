using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace AnyService.Tests
{
    public class WorkContextExtensionsTests
    {
        [Fact]
        public void GetParameterByIndex_NullParameters_RetrunsDefault()
        {
            new WorkContext().GetParameterByIndex<string>("some-key", 1).ShouldBe(default);
        }
        [Fact]
        public void GetParameterByIndex_NullParameters_ThrowsOnOutOfRange()
        {
            var key = "k";
            var wc = new WorkContext
            {
                Parameters = new Dictionary<string, object>
                {
                    {key, new[]{"eee" } }
                }
            };

            Should.Throw<ArgumentOutOfRangeException>(() => wc.GetParameterByIndex<string>(key, 1));
        }
        [Fact]
        public void GetParameterByIndex_KeyNotExists_returnsDefault()
        {
            var wc = new WorkContext
            {
                Parameters = new Dictionary<string, object> { }
            };
            wc.GetParameterByIndex<string>("not-exists", 1).ShouldBe(default);
        }
        [Fact]
        public void GetParameterByIndex_ReturnsValue()
        {
            string key = "k", value = "v";
            var wc = new WorkContext
            {
                Parameters = new Dictionary<string, object>
                {
                    { key, new[]{"ssss", value, "rr", "wer" } }
                }
            };
            wc.GetParameterByIndex<string>(key, 1).ShouldBe(value);
        }

        [Fact]
        public void GetFirstParameterByIndex_ReturnsValue()
        {
            string key = "k", value = "v";
            var wc = new WorkContext
            {
                Parameters = new Dictionary<string, object>
                {
                    { key, new[]{value, "rr", "wer" } }
                }
            };
            wc.GetFirstParameter<string>(key).ShouldBe(value);
        }
    }
}
