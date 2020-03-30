using AnyService.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace AnyService.EntityFramework
{
    public interface IAnyServiceDbContext
    {
        DbSet<UserPermissions> UserPermissions { get; set; }

    }
}