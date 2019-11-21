using System;
using AnyService.Services;
using AnyService.Services.Security;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        public TypeConfigRecord(Type type, string routePrefix, EventKeyRecord eventKeyRecord, PermissionRecord permissionRecord, string entityKey)
        {
            RoutePrefix = routePrefix;
            Type = type;
            EventKeyRecord = eventKeyRecord;
            PermissionRecord = permissionRecord;
            EntityKey = entityKey;
        }
        public string RoutePrefix { get; }
        public Type Type { get; }
        public EventKeyRecord EventKeyRecord { get; }
        public PermissionRecord PermissionRecord { get; }
        public string EntityKey { get; }
    }
}