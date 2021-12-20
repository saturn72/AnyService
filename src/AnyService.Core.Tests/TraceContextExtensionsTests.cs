using Shouldly;
using System.Diagnostics;
using Xunit;

namespace AnyService.Core.Tests
{
    public class TraceContextExtensionsTests
    {
        [Fact]
        public void TraceParentHeader()
        {
            TraceContextExtensions.TRACE_CONTEXT_TRACE_PARENT.ShouldBe("traceparent");
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void FromTraceParentHeader_OnNull_returnsDefault(string header)
        {
            var (version, traceId, parentId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(default);
            traceId.ShouldBe(default);
            parentId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersion()
        {
            var v = "ver";
            var header = v;
            var (version, traceId, parentId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(default);
            parentId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersionAndTraceId()
        {
            var v = "ver";
            var t = "trace";
            var header = $"{v}-{t}";
            var (version, traceId, parentId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            parentId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersionAndTraceIdAndParent()
        {
            var v = "ver";
            var t = "trace";
            var p = "parent";
            var header = $"{v}-{t}-{p}";
            var (version, traceId, parentId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            parentId.ShouldBe(p);
            traceFlags.ShouldBe(default);
        }

        [Fact]
        public void FromTraceParentHeader_allFieldExists()
        {
            var v = "ver";
            var t = "trace";
            var p = "parent";
            var header = $"{v}-{t}-{p}-01";
            var (version, traceId, parentId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            parentId.ShouldBe(p);
            traceFlags.ShouldBe(ActivityTraceFlags.Recorded);
        }
    }
}
