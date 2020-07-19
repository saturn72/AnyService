using System.Collections.Generic;
using AnyService.Audity;

namespace AnyService.SampleApp.Models
{
    public class DependentModel : IDomainModelBase, IFullAudit, IPublishable
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedWorkContextJson { get; set; }
        public bool Deleted { get; set; }
        public string DeletedOnUtc { get; set; }
        public string DeletedByUserId { get; set; }
        public bool Public { get; set; }
        public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
}