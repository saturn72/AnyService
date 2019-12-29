using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyService.Core.Security;
using Microsoft.AspNetCore.Http;

namespace AnyService.Middlewares
{
    public class AnyServicePermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly IReadOnlyDictionary<string, Func<TypeConfigRecord, string>> HttpMethodToPerMissionKey = new Dictionary<string, Func<TypeConfigRecord, string>>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "POST" ,t => t.PermissionRecord.CreateKey},
            {  "GET",t  => t.PermissionRecord.ReadKey},
            {   "PUT",t  => t.PermissionRecord.UpdateKey},
            {   "DELETE", t => t.PermissionRecord.DeleteKey },
        };
        public AnyServicePermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext, IPermissionManager permissionService)
        {
            if (workContext.CurrentType == null) // in-case not using Anyservice pipeline
            {
                await _next(context);
                return;
            }

            var typeConfigRecord = TypeConfigRecordManager.GetRecord(workContext.CurrentType);
            var httpMethod = context.Request.Method;
            var permissionKey = HttpMethodToPerMissionKey[httpMethod](typeConfigRecord);
            ff
            var id = context.Request.Query["id"].ToString();
            var isPost = httpMethod.Equals(HttpMethods.Post, StringComparison.InvariantCultureIgnoreCase);
            if (string.IsNullOrEmpty(id) && !isPost)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var hasPermission = isPost ?
                await permissionService.UserHasPermission(workContext.CurrentUserId, permissionKey) :
                await permissionService.UserHasPermissionOnEntity(workContext.CurrentUserId, permissionKey, typeConfigRecord.EntityKey, null);

            if (hasPermission) await _next(context);
            else context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }

    }
}
