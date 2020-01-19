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
        private static readonly TimeSpan DefaultCachingTime = TimeSpan.FromMinutes(10);

        private readonly IRepository<UserPermissions> _repository;
        private readonly ICacheManager _cacheManager;

        public PermissionManager(ICacheManager cacheManager, IRepository<UserPermissions> repository)
        {
            _cacheManager = cacheManager;
            _repository = repository;
        }
        private string GetCacheKey(string userId) => "user-permissions:" + userId;

        public async Task<UserPermissions> CreateUserPermissions(UserPermissions userPermissions)
        {
            if (!userPermissions.UserId.HasValue())
                return null;
            await _cacheManager.Remove(GetCacheKey(userPermissions.UserId));
            return await _repository.Insert(userPermissions);
        }
        public async Task<UserPermissions> GetUserPermissions(string userId)
        {
            if (!userId.HasValue())
                return null;

            var userPermissions = await _cacheManager.Get<UserPermissions>(GetCacheKey(userId));
            if (userPermissions == null)
            {
                var filter = new Dictionary<string, string> { { nameof(UserPermissions.UserId), userId } };
                userPermissions = (await _repository.GetAll(filter))?.FirstOrDefault();
                if (userPermissions != null)
                    await _cacheManager.Set(GetCacheKey(userId), userPermissions, DefaultCachingTime);
            }
            return userPermissions;
        }

        public Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions)
        {
            throw new NotImplementedException();
            //clear cache
        }
    }
}