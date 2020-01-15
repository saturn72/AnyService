using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public interface IUserPermissionsRepository
    {
        Task<UserPermissions> GetUserPermissions(string userId);
    }
}
