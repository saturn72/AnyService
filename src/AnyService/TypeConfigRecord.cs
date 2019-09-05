using System;
using AnyService.Services;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        public TypeConfigRecord(Type type, string routePrefix, EventKeyRecord eventKeyRecord)
        {
            RoutePrefix = routePrefix;
            Type = type;
            EventKeyRecord = eventKeyRecord;
        }
        public string RoutePrefix { get; }
        public Type Type { get; }
        public EventKeyRecord EventKeyRecord { get; }
    }
}