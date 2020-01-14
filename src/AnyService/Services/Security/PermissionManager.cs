using AnyService.Core.Caching;
using AnyService.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.Security
{
    public class PermissionManager : IPermissionManager
    {
        private const string UserPermissionCacheKey = "user-permissions:";
        private static readonly TimeSpan DefaultCachingTime = TimeSpan.FromMinutes(10);

        private readonly IUserPermissionsRepository _repository;
        private readonly ICacheManager _cacheManager;

        public PermissionManager(ICacheManager cacheManager, IUserPermissionsRepository repository)
        {
            _cacheManager = cacheManager;
            _repository = repository;
        }
        public async Task<bool> UserHasPermission(string userId, string permissionKey)
        {
            if (!userId.HasValue() || !permissionKey.HasValue())
                return false;

            var allUserPermissions = await GetAllUserPermissions(userId);
            return allUserPermissions != null && allUserPermissions.Any(p => p.PermissionKey.Equals(permissionKey, StringComparison.InvariantCultureIgnoreCase));
        }
        public async Task<bool> UserHasPermissionOnEntity(string userId, string permissionKey, string entityKey, string entityId)
        {
            if (!userId.HasValue() || !permissionKey.HasValue() || !entityKey.HasValue() || !entityId.HasValue())
                return false;
            var allUserPermissions = await GetAllUserPermissions(userId);
            return allUserPermissions != null && allUserPermissions.Any(p =>
                p.EntityId.HasValue() && p.EntityId.Equals(entityId, StringComparison.InvariantCultureIgnoreCase) &&
                p.EntityKey.HasValue() && p.EntityKey.Equals(entityKey, StringComparison.InvariantCultureIgnoreCase) &&
                p.PermissionKey.HasValue() && p.PermissionKey.Equals(permissionKey, StringComparison.InvariantCultureIgnoreCase));
        }

        private Task<IEnumerable<UserPermissions>> GetAllUserPermissions(string userId)
        {
            return _cacheManager.GetAsync(UserPermissionCacheKey + userId,
                () => _repository.GetUserPermissions(userId), DefaultCachingTime);
        }
    }
}
