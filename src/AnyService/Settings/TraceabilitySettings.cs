namespace AnyService.Settings
{
    public class TraceabilitySettings
    {
        public bool Active { get; set; }
        public string RequestSessionIdHeader { get; set; }
        public string RequestReferenceIdHeader { get; set; }
        public string ResponseRequestIdHeader { get; set; }
        public string ResponseTraceIdHeader { get; set; }
        public string ResponseSpanIdHeader { get; set; }
    }
}
