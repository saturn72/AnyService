using System;

namespace AnyService
{
    public sealed class WorkContext
    {
        public Type CurrentType { get; set; }
        public TypeConfigRecord CurrentTypeConfigRecord { get; set; }
        public string CurrentUserId { get; set; }
        public string RequestPath { get; set; }
        public string HttpMethod { get; set; }
    }
}