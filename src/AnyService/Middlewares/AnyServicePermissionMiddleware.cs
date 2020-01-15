using System;
using System.Threading.Tasks;
using AnyService.Core.Security;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Http;

namespace AnyService.Middlewares
{
    public class AnyServicePermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPermissionManager _permissionManager;

        public AnyServicePermissionMiddleware(RequestDelegate next, IPermissionManager permissionManager)
        {
            _next = next;
            _permissionManager = permissionManager;
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext)
        {
            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                await _next(context);
                return;
            }

            var typeConfigRecord = TypeConfigRecordManager.GetRecord(workContext.CurrentType);

            var permissionKey = PermissionFuncs.GetByHttpMethod(workContext.HttpMethod)(typeConfigRecord);
            var id = context.Request.Query["id"].ToString();
            var isPost = workContext.HttpMethod.Equals(HttpMethods.Post, StringComparison.InvariantCultureIgnoreCase);
            if (string.IsNullOrEmpty(id) && !isPost)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (await IsUserPermitted(isPost, workContext.CurrentUserId, permissionKey, typeConfigRecord))
                await _next(context);
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        private async Task<bool> IsUserPermitted(bool isPost, string userId, string permissionKey, TypeConfigRecord typeConfigRecord)
        {
            if (isPost)
            {
                var isOptimistic = typeConfigRecord.PermissionRecord.CreatePermissionStyle == PermissionStyle.Optimistic;
                return isOptimistic ?
                await _permissionManager.UserPermissionExcluded(userId, permissionKey)
                : await _permissionManager.UserHasPermission(userId, permissionKey);
            }
            return await _permissionManager.UserHasPermissionOnEntity(userId, permissionKey, typeConfigRecord.EntityKey, null);
        }
    }
}
