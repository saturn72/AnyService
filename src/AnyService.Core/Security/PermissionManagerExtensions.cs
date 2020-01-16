using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public static class PermissionManagerExtensions
    {
        public static async Task<bool> UserIsGranted(this IPermissionManager manager, string userId, string permissionKey, string entityKey, string entityId, PermissionStyle style = PermissionStyle.Optimistic)
        {
            var userPermissions = await manager.GetUserPermissions(userId);
            var isOptimistic = style == PermissionStyle.Optimistic;
            var entityPermission = userPermissions?.EntityPermissions?.FirstOrDefault(p =>
                p.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase)
                && p.EntityKey == entityKey
                && p.EntityId == entityId);

            var isExcluded = entityPermission != null && entityPermission.Excluded;

            return isOptimistic ? !isExcluded : isExcluded;
        }
    }
}
