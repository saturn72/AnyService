using System.Collections.Generic;

namespace AnyService.Core.Security
{
    public class UserPermissions : IDomainModelBase
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public virtual IEnumerable<EntityPermission> EntityPermissions { get; set; }
    }
}
