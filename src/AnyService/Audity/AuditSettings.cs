using System.ComponentModel;

namespace AnyService.Audity
{
    public class AuditSettings
    {
        [DefaultValue(true)]
        public bool Disabled { get; set; } = true;
        public AuditRules AuditRules { get; set; }
    }
}
