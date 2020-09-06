using AnyService.Audity;

namespace AnyService.SampleApp.Models
{
    public class Dependent2 : IDomainModelBase, IFullAudit, ISoftDelete
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public bool Deleted { get; set; }
    }
}