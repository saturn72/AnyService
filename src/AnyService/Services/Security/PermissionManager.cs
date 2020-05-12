using AnyService.Core.Caching;
using AnyService.Core.Security;
using System;
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
            userPermissions.CreatedOnUtc = DateTime.UtcNow;
            return await _repository.Insert(userPermissions);
        }
        public async Task<UserPermissions> GetUserPermissions(string userId)
        {
            if (!userId.HasValue())
                return null;

            var userPermissions = await _cacheManager.Get<UserPermissions>(GetCacheKey(userId));
            if (userPermissions == null)
            {
                var allUserPermissions = await _repository.GetAll(GetPaginationSettings(userId));
                userPermissions = allUserPermissions?.FirstOrDefault();
                if (userPermissions != null)
                    await _cacheManager.Set(GetCacheKey(userId), userPermissions, DefaultCachingTime);
            }
            return userPermissions;
        }

        public async Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions)
        {
            if (userPermissions == null || !userPermissions.UserId.HasValue())
                return null;

            var allUserPermissions = await _repository.GetAll(GetPaginationSettings(userPermissions.UserId));
            var dbEntity = allUserPermissions?.FirstOrDefault();

            if (dbEntity == null) return null;

            await _cacheManager.Remove(GetCacheKey(userPermissions.UserId));
            dbEntity.UpdatedOnUtc = DateTime.UtcNow;
            dbEntity.EntityPermissions = userPermissions.EntityPermissions;

            return await _repository.Update(dbEntity);
        }

        private Pagination<UserPermissions> GetPaginationSettings(string userId)
        {
            return new Pagination<UserPermissions>
            {
                QueryFunc = new Func<UserPermissions, bool>(up => up.UserId == userId),
                OrderBy = nameof(UserPermissions.CreatedOnUtc),
                IncludeNested = true,
                PageSize = ulong.MaxValue,
                SortOrder = PaginationSettings.Desc
            };
        }
    }
}