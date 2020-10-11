using AnyService.Http;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Http
{
    public class HttpHeadersNamesTests
    {
        [Fact]
        public void All()
        {
            HttpHeaderNames.ClientSessionId.ShouldBe("Session-Id");
            HttpHeaderNames.ClientRequestReference.ShouldBe("Reference");
            HttpHeaderNames.ServerResponseId.ShouldBe("Request-Id");
            HttpHeaderNames.ServerTraceId.ShouldBe("Trace-Id");
            HttpHeaderNames.ServerSpanId.ShouldBe("Span-Id");

        }
    }
}
