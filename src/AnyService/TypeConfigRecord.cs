using System;
using AnyService.Core.Security;
using AnyService.Services;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        public string RoutePrefix { get; set; }
        public Type Type { get; set; }
        public EventKeyRecord EventKeyRecord { get; set; }
        public PermissionRecord PermissionRecord { get; set; }
        public string EntityKey { get; set; }
        public ICrudValidator Validator { get; set; }
        public AuthorizationInfo Authorization { get; set; }
    }
}