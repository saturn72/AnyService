using System;
using AnyService.Services;

namespace AnyService
{
    public sealed class TypeConfigRecord
    {
        private string _controllerName;

        public TypeConfigRecord(Type type, string routePrefix, EventKeyRecord eventKeyRecord)
        {
            routePrefix = $"/{routePrefix}";
            RoutePrefix = routePrefix.Replace("//", "/");
            Type = type;
            EventKeyRecord = eventKeyRecord;
        }
        public string RoutePrefix { get; }
        public string ControllerRoute => _controllerName ?? (_controllerName = "/" + Type.Name);
        public Type Type { get; }
        public EventKeyRecord EventKeyRecord { get; }
    }
}