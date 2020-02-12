using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Core.Security
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
    }
}
