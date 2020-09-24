using System;
using System.Collections.Generic;

namespace AnyService.Models
{
    public class AuditPaginationModel:PaginationModel<AuditRecordModel>
    {
        public IEnumerable<string> AuditRecordIds { get; set; }
        public IEnumerable<string> AuditRecordTypes { get; set; }
        public IEnumerable<string> EntityNames { get; set; }
        public IEnumerable<string> EntityIds { get; set; }
        public IEnumerable<string> UserIds { get; set; }
        public IEnumerable<string> ClientIds { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}
