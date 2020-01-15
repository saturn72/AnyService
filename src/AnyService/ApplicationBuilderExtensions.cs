using System.Collections.Generic;
using AnyService.Controllers;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAnyService(this IApplicationBuilder app)
        {
            var sp = app.ApplicationServices;
            var apm = sp.GetService<ApplicationPartManager>();
            var typeConfigRecords = sp.GetService<IEnumerable<TypeConfigRecord>>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider());

            app.UseMiddleware<WorkContextMiddleware>();

            var config = sp.GetService<AnyServiceConfig>();
            if (config.ManageEntityPermissions)
            {
                app.UseMiddleware<AnyServicePermissionMiddleware>();
                throw new System.NotImplementedException("Add event listener here");

                // private async Task ManageUserPermissions(HttpContext context, string permissionKey, string entityKey)
                // {
                //     if (context.Response.StatusCode < 200 || context.Response.StatusCode > 299) //failure
                //         return;

                //     var isPost = context.Request.Method.Equals(HttpMethods.Post, StringComparison.InvariantCultureIgnoreCase);
                //     if (isPost)
                //     {
                //         var gettKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Get)(typeConfigRecord);
                //         await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, gettKey, typeConfigRecord.EntityKey, entityId);
                //         var putKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Put)(typeConfigRecord);
                //         await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, putKey, typeConfigRecord.EntityKey, entityId);
                //         var deleteKey = PermissionFuncs.GetByHttpMethod(HttpMethods.Delete)(typeConfigRecord);
                //         await _permissionManager.AddUserPermissionOnEntity(_workContext.CurrentUserId, deleteKey, typeConfigRecord.EntityKey, entityId);
                //         return;
                //     }
                //     var isDelete = context.Request.Method.Equals(HttpMethods.Delete, StringComparison.InvariantCultureIgnoreCase);
                //     if (isDelete)
                //         await _permissionManager.RemoveUserPermissionsOnEntity(_workContext.CurrentUserId, typeConfigRecord.EntityKey, entityId);
                // }
            }

            return app;
        }
    }
}
