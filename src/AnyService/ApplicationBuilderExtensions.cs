using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Controllers;
using AnyService.Caching;
using AnyService.Events;
using AnyService.Middlewares;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnyService
{

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAnyService(this IApplicationBuilder app,
            bool useWorkContextMiddleware = true,
            bool useAuthorizationMiddleware = true,
            bool useExceptionLogging = true,
            bool usePermissionMiddleware = true)
        {
            var sp = app.ApplicationServices;
            AppEngine.Init(sp);

            ValidateRequiredServices(sp);

            var apm = sp.GetRequiredService<ApplicationPartManager>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider());

            if (useWorkContextMiddleware) app.UseMiddleware<WorkContextMiddleware>();
            if (useAuthorizationMiddleware) app.UseMiddleware<DefaultAuthorizationMiddleware>();

            if (useExceptionLogging)
            {
                var entityConfigRecords = sp.GetRequiredService<IEnumerable<EntityConfigRecord>>();
                var eventKeys = entityConfigRecords.Select(e => e.EventKeys).ToArray();
                var eventBus = sp.GetRequiredService<IEventBus>();
                var exLogger = sp.GetRequiredService<ILogger<ExceptionsLoggingEventHandlers>>();
                var handlers = new ExceptionsLoggingEventHandlers(exLogger);
                foreach (var ek in eventKeys)
                {
                    eventBus.Subscribe(ek.Create, handlers.CreateEventHandler);
                    eventBus.Subscribe(ek.Read, handlers.ReadEventHandler);
                    eventBus.Subscribe(ek.Update, handlers.UpdateEventHandler);
                    eventBus.Subscribe(ek.Delete, handlers.DeleteEventHandler);
                }
            }
            if (usePermissionMiddleware) AddPermissionComponents(app, sp);
            return app;
        }
        private static void ValidateRequiredServices(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ICacheManager>();

            if (!MappingExtensions.WasConfigured)
                MappingExtensions.Configure(cfg => { });
        }
        private static void AddPermissionComponents(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<AnyServiceConfig>();
            if (!config.ManageEntityPermissions)
                return;

            app.UseMiddleware<AnyServicePermissionMiddleware>();

            //subscribe to events event listener
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var ekr = EntityConfigRecordManager.EntityConfigRecords.Select(e => e.EventKeys);
            var peh = serviceProvider.GetRequiredService<IPermissionEventsHandler>();
            foreach (var e in ekr)
            {
                eventBus.Subscribe(e.Create, peh.EntityCreatedHandler);
                eventBus.Subscribe(e.Delete, peh.EntityDeletedHandler);
            }
        }
    }
}
