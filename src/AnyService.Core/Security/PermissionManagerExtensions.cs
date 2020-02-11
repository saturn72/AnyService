using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public static class PermissionManagerExtensions
    {
        public static async Task<IEnumerable<string>> GetPermittedEntityIds(this IPermissionManager manager, string userId, string entityKey, string permissionKey)
        {
            var userPermission = await manager.GetUserPermissions(userId);
            var ids = userPermission.EntityPermissions?
                .Where(ep =>
                    ep.EntityKey.Equals(entityKey, StringComparison.InvariantCultureIgnoreCase) &&
                    !ep.Excluded &&
                    ep.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase))?
                .Select(x => x.EntityId)?
                .ToArray();

            return ids ?? new string[] { };
        }
    }
}
