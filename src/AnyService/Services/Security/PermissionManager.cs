using AnyService.Security;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.Security
{
    public class PermissionManager : IPermissionManager
    {
        private static readonly TimeSpan DefaultCachingTime = TimeSpan.FromMinutes(10);
        private readonly IRepository<UserPermissions> _repository;
        private readonly IDistributedCache _cache;

        public PermissionManager(IDistributedCache cache, IRepository<UserPermissions> repository)
        {
            _cache = cache;
            _repository = repository;
        }
        private string GetCacheKey(string userId) => "user-permissions:" + userId;

        public async Task<UserPermissions> CreateUserPermissions(UserPermissions userPermissions)
        {
            if (!userPermissions.UserId.HasValue())
                return null;

            await _cache.RemoveAsync(GetCacheKey(userPermissions.UserId));
            userPermissions.CreatedOnUtc = DateTime.UtcNow;
            return await _repository.Insert(userPermissions);
        }
        public async Task<UserPermissions> GetUserPermissions(string userId)
        {
            if (!userId.HasValue())
                return null;

            var key = GetCacheKey(userId);

            return await _cache.GetAsync<UserPermissions>(key, getPermissionRecord, DefaultCachingTime);
            async Task<UserPermissions> getPermissionRecord()
            {
                var allUserPermissions = await _repository.GetAll(GetPaginationSettings(userId));
                return allUserPermissions?.FirstOrDefault();
            }
        }

        public async Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions)
        {
            if (userPermissions == null || !userPermissions.UserId.HasValue())
                return null;

            var allUserPermissions = await _repository.GetAll(GetPaginationSettings(userPermissions.UserId));
            var dbEntity = allUserPermissions?.FirstOrDefault();

            if (dbEntity == null) return null;

            await _cache.RemoveAsync(GetCacheKey(userPermissions.UserId));
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
                PageSize = int.MaxValue,
                SortOrder = PaginationSettings.Desc
            };
        }
    }
}