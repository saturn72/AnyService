using System;
using AnyService.Services;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        public TypeConfigRecord(Type type, string routePrefix, string cacheKeyPrefix, EventKeyRecord eventKeyRecord)
        {
            RoutePrefix = routePrefix;
            Type = type;
            CacheKeyPrefix = cacheKeyPrefix;
            EventKeyRecord = eventKeyRecord;
        }
        public string RoutePrefix { get; }
        public Type Type { get; }
        public string CacheKeyPrefix { get; }
        public EventKeyRecord EventKeyRecord { get; }
    }
}