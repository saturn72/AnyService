using System.Collections.Generic;

namespace AnyService.Models
{
    public class EntityMappingRequestModel
    {
        public string ParentEntityKey { get; set; }
        public string ChildEntityKey { get; set; }
        public IEnumerable<string> Add { get; set; }
        public IEnumerable<string> Remove { get; set; }
    }
}
