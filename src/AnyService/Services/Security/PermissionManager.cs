using AnyService.Core.Caching;
using AnyService.Core.Security;
using System;
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

        public Task<UserPermissions> CreateUserPermissions(UserPermissions userPermissions)
        {
            throw new NotImplementedException();
        }

        public async Task<UserPermissions> GetUserPermissions(string userId)
        {
            if (!userId.HasValue())
                return null;

            var userPermissions = await _cacheManager.GetAsync<UserPermissions>(UserPermissionCacheKey + userId);
            if (userPermissions == null)
            {
                userPermissions = await _repository.GetUserPermissions(userId);
                if (userPermissions != null)
                    await _cacheManager.SetAsync(UserPermissionCacheKey + userId, userPermissions, DefaultCachingTime);
            }
            return userPermissions;
        }

        public Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions)
        {
            throw new NotImplementedException();
        }
    }
}