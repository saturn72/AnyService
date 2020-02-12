using System.Collections.Generic;

namespace AnyService.Core.Security
{
    public class EntityPermission : IDomainModelBase //: ChildModelBase
    {
        public string Id { get; set; }
        public bool Excluded { get; set; }
        /// <summary>
        /// Gets or sets list of string that represent access modifiers for an entity
        /// </summary>
        public IEnumerable<string> PermissionKeys { get; set; }
        public string EntityKey { get; set; }
        public string EntityId { get; set; }
    }
}
