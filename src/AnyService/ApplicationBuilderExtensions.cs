using System;
using System.Collections.Generic;
using AnyService.Controllers;
using AnyService.Caching;
using AnyService.Events;
using AnyService.Middlewares;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using AnyService.Services.Logging;

namespace AnyService
{

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAnyService(
            this IApplicationBuilder app,
            bool useWorkContextMiddleware = true,
            bool useAuthorizationMiddleware = true,
            bool logExceptions = true,
            bool usePermissionMiddleware = true)
        {
            var sp = app.ApplicationServices;
            InitializeAndValidateRequiredServices(sp);

            var apm = sp.GetRequiredService<ApplicationPartManager>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider(sp));

            if (useWorkContextMiddleware) app.UseMiddleware<WorkContextMiddleware>();
            if (useAuthorizationMiddleware) app.UseMiddleware<DefaultAuthorizationMiddleware>();

            if (logExceptions)
            {
                var entityConfigRecords = sp.GetRequiredService<IEnumerable<EntityConfigRecord>>();
                var eventKeys = entityConfigRecords.Select(e => e.EventKeys).ToArray();
                var eventBus = sp.GetRequiredService<IEventBus>();
                var exLogger = sp.GetRequiredService<ILogger<ExceptionsLoggingEventHandlers>>();
                var handlers = new ExceptionsLoggingEventHandlers(exLogger);
                foreach (var ek in eventKeys)
                {
                    eventBus.Subscribe(ek.Create, handlers.CreateEventHandler, "domain-object-handler-created");
                    eventBus.Subscribe(ek.Read, handlers.ReadEventHandler, "domain-object-handler-read");
                    eventBus.Subscribe(ek.Update, handlers.UpdateEventHandler, "domain-object-handler-updated");
                    eventBus.Subscribe(ek.Delete, handlers.DeleteEventHandler, "domain-object-handler-deleted");
                }
            }
            if (usePermissionMiddleware) AddPermissionComponents(app, sp);
            MappingExtensions.Configure();
            return app;
        }
        private static void InitializeAndValidateRequiredServices(IServiceProvider serviceProvider)
        {
            ServiceResponseWrapperExtensions.Init(serviceProvider);
            GenericControllerNameConvention.Init(serviceProvider);
            serviceProvider.GetRequiredService<ICacheManager>();
        }
       
        private static void AddPermissionComponents(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetRequiredService<AnyServiceConfig>();
            if (!config.ManageEntityPermissions)
                return;

            app.UseMiddleware<AnyServicePermissionMiddleware>();

            //subscribe to events event listener
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var ecrm = serviceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();
            var ekr = ecrm.Select(e => e.EventKeys);
            var peh = serviceProvider.GetRequiredService<IPermissionEventsHandler>();
            foreach (var e in ekr)
            {
                eventBus.Subscribe(e.Create, peh.EntityCreatedHandler, "entity-created-permission-handler");
                eventBus.Subscribe(e.Delete, peh.EntityDeletedHandler, "entity-deleted-permission-handler");
            }
        }
    }
}
