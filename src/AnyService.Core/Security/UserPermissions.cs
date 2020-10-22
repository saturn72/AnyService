using System;
using System.Collections.Generic;

namespace AnyService.Security
{
    public class UserPermissions : IEntity
    {
        private IEnumerable<EntityPermission> _entityPermissions;
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public virtual IEnumerable<EntityPermission> EntityPermissions
        {
            get { return _entityPermissions ?? (_entityPermissions = new List<EntityPermission>()); }
            set { _entityPermissions = value; }
        }
    }
}
