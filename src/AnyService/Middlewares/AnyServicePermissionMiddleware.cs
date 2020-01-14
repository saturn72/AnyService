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
            _permissionManager = permissionManager
        }

        public async Task InvokeAsync(HttpContext context, WorkContext workContext)
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
                await _permissionManager.UserHasPermission(workContext.CurrentUserId, permissionKey) :
                await _permissionManager.UserHasPermissionOnEntity(workContext.CurrentUserId, permissionKey, typeConfigRecord.EntityKey, null);

            if (hasPermission) await _next(context);
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var t = ManageUserPermissions(context, permissionKey, typeConfigRecord.EntityKey);
        }

        private async Task ManageUserPermissions(HttpContext context, string permissionKey, string entityKey)
        {
            if (context.Response.StatusCode < 200 || context.Response.StatusCode > 299) //failure
                return;

            var isPost = context.Request.Method.Equals(HttpMethods.Post, StringComparison.InvariantCultureIgnoreCase);
            if (isPost)
            {
                var gettKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Get)(typeConfigRecord);
                await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, gettKey, typeConfigRecord.EntityKey, entityId);
                var putKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Put)(typeConfigRecord);
                await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, putKey, typeConfigRecord.EntityKey, entityId);
                var deleteKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Delete)(typeConfigRecord);
                await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, deleteKey, typeConfigRecord.EntityKey, entityId);
                return;
            }
            var isDelete = context.Request.Method.Equals(HttpMethods.Delete, StringComparison.InvariantCultureIgnoreCase);
            if (isDelete)
                await _permissionManager.RemoveUserPermissionsOnEntity(_workContext.CurrentUserId, typeConfigRecord.EntityKey, entityId);
        }
    }
}
