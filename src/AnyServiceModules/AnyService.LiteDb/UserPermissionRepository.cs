using System.Threading.Tasks;
using AnyService.Core.Security;

namespace AnyService.LiteDb
{
    public class UserPermissionRepository : IUserPermissionsRepository
    {
        private readonly string _dbName;
        public UserPermissionRepository(string dbName)
        {
            _dbName = dbName;
        }
        public async Task<UserPermissions> GetUserPermissions(string userId)
        {
            return await Task.Run(() => LiteDbUtility.Query(_dbName, db => db.GetCollection<UserPermissions>().FindOne(up => up.UserId == userId)));
        }
    }
}
