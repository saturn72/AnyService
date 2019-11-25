using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core.Security;

namespace AnyService.LiteDb
{
    public class UserPermissionRepository : IUserPermissionsRepository
    {
        public Task<IEnumerable<UserPermissions>> GetUserPermissions(string userId)
        {
            throw new System.NotImplementedException();
        }
    }
}
