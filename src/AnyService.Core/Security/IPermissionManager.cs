using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public interface IPermissionManager
    {
        Task<UserPermissions> CreateUserPermissions(UserPermissions userPermissions);
        Task<UserPermissions> GetUserPermissions(string userId);
        Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions);
    }
}
