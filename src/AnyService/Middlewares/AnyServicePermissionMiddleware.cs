using System;
using System.Linq;
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

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                await _next(httpContext);
                return;
            }

            var typeConfigRecord = TypeConfigRecordManager.GetRecord(workContext.CurrentType);

            var permissionKey = PermissionFuncs.GetByHttpMethod(workContext.RequestInfo.Method)(typeConfigRecord);
            var id = workContext.RequestInfo.RequesteeId;

            var isGet = HttpMethods.IsGet(workContext.RequestInfo.Method);
            var isPost = HttpMethods.IsPost(workContext.RequestInfo.Method);

            if (string.IsNullOrEmpty(id) && !isPost && !isGet)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var isGranted = await IsGranted(workContext.CurrentUserId, permissionKey, typeConfigRecord.EntityKey, isPost ? null : id, isPost);
            if (!isGranted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await _next(httpContext);
        }

        public async Task<bool> IsGranted(string userId, string permissionKey, string entityKey, string entityId, bool isPost)
        {
            var userPermissions = await _permissionManager.GetUserPermissions(userId);

            var entityPermission = userPermissions?.EntityPermissions?.FirstOrDefault(p =>
                p.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase)
                && p.EntityKey == entityKey
                && p.EntityId == entityId);

            return (entityPermission == null && isPost) || (entityPermission != null && !entityPermission.Excluded);
        }
    }
}
