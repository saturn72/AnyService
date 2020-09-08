using System;

namespace AnyService.Audity
{
    public class AuditSettings
    {
        public Func<Type, string> EntityNameResolver { get; set; }
        public AuditRules AuditRules { get; set; }
    }
}
