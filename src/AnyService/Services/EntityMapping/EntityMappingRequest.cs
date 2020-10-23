using System.Collections.Generic;

namespace AnyService.Services.EntityMapping
{
    public class EntityMappingRequest
    {
        public string ParentEntityKey { get; set; }
        public string ParentId { get; set; }
        public string ChildEntityKey { get; set; }
        public IEnumerable<string> Add { get; set; }
        public IEnumerable<string> Remove { get; set; }
    }
}
