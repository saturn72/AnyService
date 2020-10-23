using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Security
{
    public static class PermissionManagerExtensions
    {
        public static async Task<bool> UserHasPermissionOnEntity(this IPermissionManager manager, string userId, string entityKey, string permissionKey, string entityId)
        {
            var userPermissions = await manager.GetUserPermissions(userId);
            var hasPermission = userPermissions?
                .EntityPermissions?
                .FirstOrDefault(p => p.EntityId.Equals(entityId, StringComparison.InvariantCultureIgnoreCase)
                    && p.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase)
                    && p.EntityKey.Equals(entityKey, StringComparison.InvariantCultureIgnoreCase));
            return !hasPermission?.Excluded ?? false;
        }
        public static async Task<IEnumerable<string>> GetPermittedEntitiesIds(this IPermissionManager manager, string userId, string entityKey, string permissionKey)
        {
            var ups = await manager.GetUserPermissions(userId);
            return ups?.EntityPermissions?
                .Where(ep => ep.EntityKey == entityKey && !ep.Excluded && ep.PermissionKeys.Contains(permissionKey))?
                .Select(e => e.EntityId).ToArray() ?? new string[] { };
        }
    }
}
