namespace System.Diagnostics
{
    public static class OpenTelemetry
    {
        //https://w3c.github.io/trace-context/#traceparent-header
        public const string OPEN_TELEMETRY_TRACE_PARENT = "traceparent";
        private const string OPEN_TELEMETRY_VERSION = "version";
        private const string OPEN_TELEMETRY_TRACE_ID = "trace-id";
        private const string OPEN_TELEMETRY_PARENT_SPAN_ID = "parent-id";
        private const string OPEN_TELEMETRY_TRACE_FLAGS = "trace-flags";

        public static string ToOpenTelemetryTraceParentHeaderValue(this Activity activity) => $"00-{activity.TraceId}-{activity.SpanId}-{activity.ActivityTraceFlags}";
        public static (string version, string traceId, string parentId, ActivityTraceFlags traceFlags) ToTraceParent(this string header)
        {
            if (!header.HasValue())
                return default;
            var a = header.Split('-');

            switch (a.Length)
            {
                case 1:
                    return (a[0], default, default, default);
                case 2:
                    return (a[0], a[1], default, default);
                case 3:
                    return (a[0], a[1], a[2], default);
                default:
                    Enum.TryParse<ActivityTraceFlags>(a[3], out var flags);
                    return (a[0], a[1], a[2], flags);
            }
        }
    }
}
