
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public interface IUserPermissionsRepository
    {
        Task<IEnumerable<UserPermissions>> GetUserPermissions(string userId);
    }
}
