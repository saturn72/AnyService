using System.Collections.Generic;

namespace AnyService.Core.Security
{
    public class UserPermissions : IDomainModelBase
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public IEnumerable<EntityPermission> EntityPermission { get; set; }
    }
    public class EntityPermission : ChildModelBase
    {
        public string PermissionKey { get; set; }
        public string EntityKey { get; set; }
        public string EntityId { get; set; }
    }
}
