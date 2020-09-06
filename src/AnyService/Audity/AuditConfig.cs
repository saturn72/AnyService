using System;

namespace AnyService.Audity
{
    public class AuditConfig
    {
        public Func<Type, string> EntityNameResolver { get; set; }
    }
}
