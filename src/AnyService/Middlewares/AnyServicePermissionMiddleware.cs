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

            var cfgRecord = workContext.CurrentEntityConfigRecord;
            var reqInfo = workContext.RequestInfo;
            var entityId = reqInfo.RequesteeId;
            var isPost = HttpMethods.IsPost(reqInfo.Method);
            var isGet = HttpMethods.IsGet(reqInfo.Method);
            if (string.IsNullOrEmpty(reqInfo.RequesteeId) && !isPost && !isGet)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            //post, get-all and get-by-id when publicGet==true are always permitted 
            var isGranted = isPost ||
                 (isGet && (!entityId.HasValue() || cfgRecord.PublicGet)) ||
                 await IsGranted(workContext);

            if (!isGranted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await _next(httpContext);
        }

        protected async Task<bool> IsGranted(WorkContext workContext)
        {
            var cfgRecord = workContext.CurrentEntityConfigRecord;
            var reqInfo = workContext.RequestInfo;
            var isGet = HttpMethods.IsGet(reqInfo.Method);

            var entityId = reqInfo.RequesteeId;

            var userId = workContext.CurrentUserId;
            var permissionKey = PermissionFuncs.GetByHttpMethod(reqInfo.Method)(cfgRecord);
            var entityKey = cfgRecord.EntityKey;
            if (isGet || HttpMethods.IsPut(reqInfo.Method) || HttpMethods.IsDelete(reqInfo.Method))
                return await _permissionManager.UserHasPermissionOnEntity(userId, entityKey, permissionKey, entityId);

            throw new NotSupportedException("http method is not supported");
        }
    }
}
