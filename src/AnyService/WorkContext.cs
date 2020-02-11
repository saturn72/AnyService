using System;

namespace AnyService
{
    public sealed class WorkContext
    {
        public Type CurrentType { get; set; }
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string CurrentUserId { get; set; }
        public RequestInfo RequestInfo { get; set; }
    }
}