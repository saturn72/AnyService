using System;

namespace AnyService.Audity
{
    public class AuditSettings
    {
        public bool Active { get; set; }
        public Func<Type, string> EntityNameResolver { get; set; }
        public AuditRules AuditRules { get; set; }
    }
}
