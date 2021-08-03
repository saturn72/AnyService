using System.ComponentModel;

namespace AnyService.Audity
{
    public class AuditSettings
    {
        [DefaultValue(false)]
        public bool Enabled { get; set; }
        public AuditRules AuditRules { get; set; }
    }
}
