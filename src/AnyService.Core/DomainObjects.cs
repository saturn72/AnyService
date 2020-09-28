using System.Collections.Generic;

namespace AnyService
{
    public interface IDomainObject
    {
        string Id { get; set; }
    }
   
    public abstract class ChildModelBase : IDomainObject
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}