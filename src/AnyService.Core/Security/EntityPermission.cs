using System.Collections.Generic;

namespace AnyService.Core.Security
{
    public class EntityPermission : IDomainModelBase //: ChildModelBase
    {
        public string Id { get; set; }
        public bool Excluded { get; set; }
        public IEnumerable<string> PermissionKeys { get; set; }
        public string EntityKey { get; set; }
        public string EntityId { get; set; }
    }
}
