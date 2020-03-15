using System;
using System.Threading.Tasks;
using AnyService.Core.Security;
using AnyService.Services;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public class AnyServicePermissionMiddleware
    {
        private const string PublicSuffix = "/" + Consts.PublicSuffix;
        private readonly ILogger<AnyServicePermissionMiddleware> _logger;
        private readonly RequestDelegate _next;
        public AnyServicePermissionMiddleware(RequestDelegate next, ILogger<AnyServicePermissionMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, WorkContext workContext, IPermissionManager permissionManager)
        {
            _logger.LogDebug(LoggingEvents.Permission, "Start AnyServicePermissionMiddleware invokation");

            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                _logger.LogDebug(LoggingEvents.Permission, "Skip anyservice middleware");

                await _next(httpContext);
                return;
            }

            var reqInfo = workContext.RequestInfo;
            var entityId = reqInfo.RequesteeId;
            var httpMethodParse = IsSupported(reqInfo.Method);

            if (!httpMethodParse.IsSupported ||
                (string.IsNullOrEmpty(reqInfo.RequesteeId) && !httpMethodParse.IsPost && !httpMethodParse.IsGet))
            {
                var msgSuffix = httpMethodParse.IsSupported ?
                    "Missing entity id in request that requires it" :
                     "Not supported http method";
                _logger.LogDebug(LoggingEvents.Permission, "Bad request due to " + msgSuffix);

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            //post, get-all and get-by-id when publicGet==true are always permitted 
            var isGranted = httpMethodParse.IsPost || IsPublicGet(httpMethodParse.IsGet, workContext, reqInfo) ||
                await IsGranted(workContext, permissionManager);

            if (!isGranted)
            {
                _logger.LogDebug(LoggingEvents.Permission, "User is not permitted to perform this operation");

                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            _logger.LogDebug(LoggingEvents.Permission, "User is permitted to perform this operation. Move to next middleware");
            await _next(httpContext);
        }

        private bool IsPublicGet(bool isGet, WorkContext workContext, RequestInfo reqInfo)
        {
            return isGet && workContext.CurrentEntityConfigRecord.PublicGet &&
            //public get all
           ((reqInfo.Path.HasValue() && reqInfo.Path.EndsWith(PublicSuffix)) ||
            //get by id
            workContext.RequestInfo.RequesteeId.HasValue());
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

        protected async Task<bool> IsGranted(WorkContext workContext, IPermissionManager permissionManager)
        {
            var cfgRecord = workContext.CurrentEntityConfigRecord;
            var reqInfo = workContext.RequestInfo;
            var isGet = HttpMethods.IsGet(reqInfo.Method);

            var entityId = reqInfo.RequesteeId;

            var userId = workContext.CurrentUserId;
            var permissionKey = PermissionFuncs.GetByHttpMethod(reqInfo.Method)(cfgRecord);
            var entityKey = cfgRecord.EntityKey;
            if (isGet || HttpMethods.IsPut(reqInfo.Method) || HttpMethods.IsDelete(reqInfo.Method))
                return await permissionManager.UserHasPermissionOnEntity(userId, entityKey, permissionKey, entityId);
            return false;
        }
    }
}
