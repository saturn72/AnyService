using System;
using AnyService.Services;
using AnyService.Core.Security;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        public string RoutePrefix { get; set; }
        public Type Type { get; set; }
        public EventKeyRecord EventKeyRecord { get; set; }
        public PermissionRecord PermissionRecord { get; set; }
        public string EntityKey { get; set; }
    }
}