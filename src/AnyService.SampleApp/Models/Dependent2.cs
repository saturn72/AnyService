using System.Collections.Generic;
using AnyService.Services;
using AnyService.Audity;

namespace AnyService.SampleApp.Models
{
    public class Dependent2 : IDomainModelBase, IFullAudit
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public string CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public bool Deleted { get; set; }
        public string DeletedOnUtc { get; set; }
        public string DeletedByUserId { get; set; }
        public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
    }
}