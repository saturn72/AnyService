using System.Collections.Generic;

namespace AnyService.Audity
{
    public interface IUpdatableAudit
    {
        IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
    public class UpdateRecord
    {
        public string UpdatedOnUtc { get; set; }
        public string UpdatedByUserId { get; set; }
    }
}