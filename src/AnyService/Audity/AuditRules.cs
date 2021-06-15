using System.ComponentModel;

namespace AnyService.Audity
{
    public class AuditRules
    {
        [DefaultValue(false)]
        public bool AuditCreate { get; set; }
        [DefaultValue(false)]
        public bool AuditRead { get; set; }
        [DefaultValue(false)]
        public bool AuditUpdate { get; set; }
        [DefaultValue(false)]
        public bool AuditDelete { get; set; }
    }
}
