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

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext)
        {
            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                await _next(httpContext);
                return;
            }

            var typeConfigRecord = TypeConfigRecordManager.GetRecord(workContext.CurrentType);

            var permissionKey = PermissionFuncs.GetByHttpMethod(workContext.RequestInfo.Method)(typeConfigRecord);
            var id = workContext.RequestInfo.RequesteeId;//.Request.Query["id"].ToString();

            var isGet = HttpMethods.IsGet(workContext.RequestInfo.Method);
            var isPost = HttpMethods.IsPost(workContext.RequestInfo.Method);

            if (string.IsNullOrEmpty(id) && !isPost && !isGet)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (await _permissionManager.UserIsGranted(
                workContext.CurrentUserId,
                permissionKey,
                typeConfigRecord.EntityKey,
                isPost ? null : id,
                typeConfigRecord.PermissionRecord.CreatePermissionStyle))
            {
                await _next(httpContext);
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
    }
}
