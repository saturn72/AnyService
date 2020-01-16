using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public interface IPermissionManager
    {
        Task<UserPermissions> GetUserPermissions(string userId);
        Task<UserPermissions> UpdateUserPermissions(UserPermissions userPermissions);
    }
}
