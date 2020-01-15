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

            if (await _permissionManager.UserIsGranted(
                workContext.CurrentUserId,
                permissionKey,
                typeConfigRecord.EntityKey,
                isPost ? null : id,
                typeConfigRecord.PermissionRecord.CreatePermissionStyle))
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
    }
}
