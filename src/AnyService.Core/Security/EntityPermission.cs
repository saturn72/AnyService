using System.Collections.Generic;

namespace AnyService.Security
{
    public class EntityPermission : IEntity
    {
        public string Id { get; set; }
        public bool Excluded { get; set; }
        /// <summary>
        /// Gets or sets list of string that represent access modifiers for an entity
        /// </summary>
        public virtual IEnumerable<string> PermissionKeys { get; set; }
        public string EntityKey { get; set; }
        public string EntityId { get; set; }
    }
}
