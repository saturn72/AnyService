using System;

namespace AnyService
{
    public sealed class WorkContext
    {
        public Type CurrentType => CurrentEntityConfigRecord.Type;
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string CurrentUserId { get; set; }
        public RequestInfo RequestInfo { get; set; }
    }
}