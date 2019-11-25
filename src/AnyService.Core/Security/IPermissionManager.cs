﻿using System.Threading.Tasks;

namespace AnyService.Core.Security
{
    public interface IPermissionManager
    {
        Task<bool> UserHasPermission( string userId, string permissionKey);
        Task<bool> UserHasPermissionOnEntity(string userId, string permissionKey, string entityKey, string entityId);
    }
}