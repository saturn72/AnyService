namespace AnyService.Http
{
    public sealed class HttpHeaderNames
    {
        public const string ClientSessionId = "Session-Id";
        public const string ClientRequestReference = "Reference";

        public const string ServerResponseId = "Request-Id";
        public const string ServerTraceId = "Trace-Id";
        public const string ServerSpanId = "Span-Id";
    }
}
