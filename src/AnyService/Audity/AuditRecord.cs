using System;

namespace AnyService.Audity
{
    public class AuditRecord : IDomainModelBase
    {
        public string Id { get; set; }
        public string AuditRecordType { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Iso8601Utc { get; set; }
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public string Data { get; set; }
    }
}
