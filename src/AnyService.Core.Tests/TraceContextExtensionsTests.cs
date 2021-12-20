﻿using Shouldly;
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
        [InlineData(ActivityTraceFlags.None, "0", null, "00")]
        [InlineData(ActivityTraceFlags.None, "0", "123", "123")]
        [InlineData(ActivityTraceFlags.Recorded, "1", null, "00")]
        [InlineData(ActivityTraceFlags.Recorded, "1", "123", "123")]
        public void ToTraceParentHeaderValue(ActivityTraceFlags flags, string expFlag, string version, string expVersion)
        {
            //https://www.w3.org/TR/trace-context/#examples-of-http-traceparent-headers
            string traceId = "4bf92f3577b34da6a3ce929d0e0e4736",
                parentSpanId = "00f067aa0ba902b7";

            var a = new Activity("test");
            var atId = ActivityTraceId.CreateFromString(traceId);
            var asId = ActivitySpanId.CreateFromString(parentSpanId);
            a.SetParentId(atId, asId, flags);

            var v = TraceContextExtensions.ToTraceParentHeaderValue(a, version);
            v.ShouldBe($"{expVersion}-{traceId}-0000000000000000-0{expFlag}");
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void FromTraceParentHeader_OnNull_returnsDefault(string header)
        {
            var (version, traceId, spanId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(default);
            traceId.ShouldBe(default);
            spanId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersion()
        {
            var v = "ver";
            var header = v;
            var (version, traceId, spanId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(default);
            spanId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersionAndTraceId()
        {
            var v = "ver";
            var t = "trace";
            var header = $"{v}-{t}";
            var (version, traceId, spanId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            spanId.ShouldBe(default);
            traceFlags.ShouldBe(default);
        }
        [Fact]
        public void FromTraceParentHeader_OnOnlyVersionAndTraceIdAndParent()
        {
            var v = "ver";
            var t = "trace";
            var s = "span";
            var header = $"{v}-{t}-{s}";
            var (version, traceId, spanId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            spanId.ShouldBe(s);
            traceFlags.ShouldBe(default);
        }

        [Fact]
        public void FromTraceParentHeader_allFieldExists()
        {
            var v = "ver";
            var t = "trace";
            var s = "span";
            var header = $"{v}-{t}-{s}-01";
            var (version, traceId, spanId, traceFlags) = header.FromTraceParentHeader();

            version.ShouldBe(v);
            traceId.ShouldBe(t);
            spanId.ShouldBe(s);
            traceFlags.ShouldBe(ActivityTraceFlags.Recorded);
        }
    }
}