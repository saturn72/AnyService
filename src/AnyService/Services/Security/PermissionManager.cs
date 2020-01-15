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
        public Task<UserPermissions> GetUserPermissions(string userId)
        {
            if (!userId.HasValue())
                return null;

            return _cacheManager.GetAsync(UserPermissionCacheKey + userId, () => _repository.GetUserPermissions(userId), DefaultCachingTime);
        }
    }
}
