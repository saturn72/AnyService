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

            var reqInfo = workContext.RequestInfo;
            var entityId = reqInfo.RequesteeId;
            var httpMethodParse = IsSupported(reqInfo.Method);

            if (!httpMethodParse.IsSupported || (string.IsNullOrEmpty(reqInfo.RequesteeId) && !httpMethodParse.IsPost && !httpMethodParse.IsGet))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            //post, get-all and get-by-id when publicGet==true are always permitted 
            var isGranted = httpMethodParse.IsPost ||
                (httpMethodParse.IsGet && (!entityId.HasValue() || workContext.CurrentEntityConfigRecord.PublicGet)) ||
                await IsGranted(workContext);

            if (!isGranted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await _next(httpContext);
        }

        private (bool IsSupported, bool IsPost, bool IsGet) IsSupported(string method)
        {
            var isPost = HttpMethods.IsPost(method);
            var isGet = HttpMethods.IsGet(method);
            var isSupported =
                isPost ||
                isGet ||
                HttpMethods.IsPut(method) ||
                HttpMethods.IsDelete(method);

            return (isSupported, isPost, isGet);
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
            return false;
        }
    }
}
