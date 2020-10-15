using System.Collections.Generic;

namespace AnyService.Services
{
    public class EntityMappingRequest
    {
        public string ParentId { get; set; }
        public string ChildEntityName { get; set; }
        public IEnumerable<string> ChildIdsToAdd { get; set; }
        public IEnumerable<string> ChildIdsToRemove { get; set; }
    }
}
