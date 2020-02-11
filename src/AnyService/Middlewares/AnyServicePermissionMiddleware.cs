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

            var typeConfigRecord = EntityConfigRecordManager.GetRecord(workContext.CurrentType);

            var permissionKey = PermissionFuncs.GetByHttpMethod(workContext.RequestInfo.Method)(typeConfigRecord);
            var id = workContext.RequestInfo.RequesteeId;

            var isGet = HttpMethods.IsGet(workContext.RequestInfo.Method);
            var isPost = HttpMethods.IsPost(workContext.RequestInfo.Method);

            if (string.IsNullOrEmpty(id) && !isPost && !isGet)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var isGranted = await IsGranted(workContext.CurrentUserId, permissionKey, typeConfigRecord.EntityId, isPost ? "" : id, isPost);
            if (!isGranted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await _next(httpContext);
        }

        private async Task<bool> IsGranted(string userId, string permissionKey, string entityKey, string entityId, bool isPost)
        {
            var userPermissions = await _permissionManager.GetUserPermissions(userId);
            var allPermissions = userPermissions?.EntityPermissions?.Where(p =>
                            p.PermissionKeys.Contains(permissionKey, StringComparer.InvariantCultureIgnoreCase)
                            && p.EntityKey.Equals(entityKey, StringComparison.InvariantCultureIgnoreCase));
            var specificQuery = entityId.HasValue() ?
                        new Func<EntityPermission, bool>(u => u.EntityId.Equals(entityId, StringComparison.InvariantCultureIgnoreCase)) :
                        new Func<EntityPermission, bool>(u => true);

            var entityPermission = allPermissions?.FirstOrDefault(specificQuery);

            return (entityPermission == null && isPost) || (entityPermission != null && !entityPermission.Excluded);
        }
    }
}
