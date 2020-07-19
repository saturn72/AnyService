using System.Collections.Generic;

namespace AnyService.Audity
{
    public interface IUpdatableAudit
    {
        IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
    public class UpdateRecord : IDomainModelBase
    {
        public string Id { get; set; }
        public string UpdatedOnUtc { get; set; }
        public string UpdatedByUserId { get; set; }
        public string WorkContextJson { get; set; }
    }
}