using System.Collections.Generic;
using System.Linq;
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
            var types = typeConfigRecords.Select(e => e.Type).ToArray();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider(types));

            app.UseMiddleware<AnyServiceWorkContextMiddleware>();
            // app.UseMiddleware<AnyServicePermissionMiddleware>();

            return app;
        }
    }
}
