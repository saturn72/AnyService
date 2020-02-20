using System.Collections.Generic;
using AnyService.Core;

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
    }
}