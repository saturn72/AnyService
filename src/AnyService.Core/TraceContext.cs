﻿namespace System.Diagnostics
{
    public static class TraceContext
    {
        //https://w3c.github.io/trace-context/#traceparent-header
        public const string TRACE_CONTEXT_TRACE_PARENT = "traceparent";
        public static string ToTraceParentHeaderValue(this Activity activity, string version = null) =>  $"{version ??"00"}-{activity.TraceId}-{activity.ParentSpanId}-0{(int)activity.ActivityTraceFlags}";

        public static (string version, string traceId, string parentId, ActivityTraceFlags traceFlags) FromTraceParentHeader(this string header)
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
