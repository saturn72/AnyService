﻿using System;
using System.Threading.Tasks;
using AnyService.Security;
using AnyService.Services;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AnyService.Middlewares
{
    public class AnyServicePermissionMiddleware
    {
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
            //post, get-all and get-by-id when publicGet==true are always permitted 
            var isGranted = await IsGranted(workContext, permissionManager);
            if (!isGranted)
            {
                _logger.LogDebug(LoggingEvents.Permission, "User is not permitted to perform this operation");
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            _logger.LogDebug(LoggingEvents.Permission, "User is permitted to perform this operation. Move to next middleware");
            await _next(httpContext);
        }

        protected async Task<bool> IsGranted(WorkContext workContext, IPermissionManager permissionManager)
        {
            var reqInfo = workContext.RequestInfo;
            var cfgRecord = workContext.CurrentEntityConfigRecord;
            _logger.LogDebug($"Request requires entity Id valued: {reqInfo.RequesteeId}");

            if (HttpMethods.IsPost(reqInfo.Method) ||
                (HttpMethods.IsGet(reqInfo.Method) && !reqInfo.RequesteeId.HasValue()))
            {
                _logger.LogDebug("User is granted - get all and post are always granted");
                return true;
            }

            _logger.LogDebug("Check usesr permissions");
            var userId = workContext.CurrentUserId;
            _logger.LogDebug($"Request requires permissions for user Id valued: {userId}");
            var entityKey = cfgRecord.EntityKey;
            _logger.LogDebug($"Request requires entity key valued: {entityKey}");
            var permissionFunc = PermissionFuncs.GetByHttpMethod(reqInfo.Method);
            if (permissionFunc == null)
                return false;

            var permissionKey = permissionFunc(cfgRecord);
            _logger.LogDebug($"Request requires permission key valued: {permissionKey}");

            return await permissionManager.UserHasPermissionOnEntity(userId, entityKey, permissionKey, reqInfo.RequesteeId);
        }
    }
}
