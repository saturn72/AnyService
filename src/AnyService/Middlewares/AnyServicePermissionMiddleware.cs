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
        public AnyServicePermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext, IPermissionManager permissionManager)
        {
            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                await _next(context);
                return;
            }

            var typeConfigRecord = TypeConfigRecordManager.GetRecord(workContext.CurrentType);

            var httpMethod = context.Request.Method;
            var permissionKey = PermissionFuncs.GetByHttpMethod(httpMethod)(typeConfigRecord);
            var id = context.Request.Query["id"].ToString();
            var isPost = httpMethod.Equals(HttpMethods.Post, StringComparison.InvariantCultureIgnoreCase);
            if (string.IsNullOrEmpty(id) && !isPost)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var hasPermission = isPost ?
                await permissionManager.UserHasPermission(workContext.CurrentUserId, permissionKey) :
                await permissionManager.UserHasPermissionOnEntity(workContext.CurrentUserId, permissionKey, typeConfigRecord.EntityKey, null);

            if (hasPermission) await _next(context);
            else context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

    }
}
