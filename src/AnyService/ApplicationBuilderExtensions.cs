﻿using System;
using System.Collections.Generic;
using AnyService.Controllers;
using AnyService.Caching;
using AnyService.Events;
using AnyService.Middlewares;
using AnyService.Services.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using AnyService.Services.Logging;
using AnyService.Audity;
using AnyService.Services.Audit;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

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
            using var scope = sp.CreateScope();
            var apm = scope.ServiceProvider.GetRequiredService<ApplicationPartManager>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider(sp));

            InitializeServices(sp);
            var cm = scope.ServiceProvider.GetRequiredService<ICacheManager>();
            var entityConfigRecords = sp.GetRequiredService<IEnumerable<EntityConfigRecord>>();
            var eventBus = sp.GetRequiredService<IEventBus>();

            if (useWorkContextMiddleware) app.UseMiddleware<WorkContextMiddleware>();
            if (useAuthorizationMiddleware) app.UseMiddleware<DefaultAuthorizationMiddleware>();

            if (logExceptions) SubscribeLogExceptionHandler(eventBus, sp, entityConfigRecords);
            SubscribeAuditHandler(sp, eventBus, entityConfigRecords);

            if (usePermissionMiddleware)
                AddPermissionComponents(app, sp, eventBus, entityConfigRecords);
            return app;
        }

        private static void SubscribeAuditHandler(IServiceProvider sp, IEventBus eventBus, IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            var auditHandler = new AuditHandler(sp);

            var creatables = entityConfigRecords.Where(ecr => ecr.Type.IsOfType<ICreatableAudit>() && !ecr.AuditSettings.Disabled && ecr.AuditSettings.AuditRules.AuditCreate).ToArray();
            foreach (var ca in creatables)
                eventBus.Subscribe(ca.EventKeys.Create, auditHandler.CreateEventHandler, $"{ca.Name.ToLower()}-creatable-audit-handler");

            var readables = entityConfigRecords.Where(ecr => ecr.Type.IsOfType<IReadableAudit>() && !ecr.AuditSettings.Disabled && ecr.AuditSettings.AuditRules.AuditRead).ToArray();
            foreach (var ca in readables)
                eventBus.Subscribe(ca.EventKeys.Read, auditHandler.ReadEventHandler, $"{ca.Name.ToLower()}-read-audit-handler");

            var updatables = entityConfigRecords.Where(ecr => ecr.Type.IsOfType<IUpdatableAudit>() && !ecr.AuditSettings.Disabled && ecr.AuditSettings.AuditRules.AuditUpdate).ToArray();
            foreach (var ca in updatables)
                eventBus.Subscribe(ca.EventKeys.Update, auditHandler.UpdateEventHandler, $"{ca.Name.ToLower()}-updatable-audit-handler");

            var deletables = entityConfigRecords.Where(ecr => ecr.Type.IsOfType<IDeletableAudit>() && !ecr.AuditSettings.Disabled && ecr.AuditSettings.AuditRules.AuditDelete).ToArray();
            foreach (var ca in deletables)
                eventBus.Subscribe(ca.EventKeys.Delete, auditHandler.DeleteEventHandler, $"{ca.Name.ToLower()}-deletable-audit-handler");
        }

        private static void SubscribeLogExceptionHandler(IEventBus eventBus, IServiceProvider sp, IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            var handlers = new ExceptionsLoggingEventHandlers(sp);
            foreach (var ecr in entityConfigRecords)
            {
                eventBus.Subscribe(ecr.EventKeys.Create, handlers.CreateEventHandler, $"{ecr.Name.ToLower()}-domain-entity-handler-created");
                eventBus.Subscribe(ecr.EventKeys.Read, handlers.ReadEventHandler, $"{ecr.Name.ToLower()}-domain-entity-handler-read");
                eventBus.Subscribe(ecr.EventKeys.Update, handlers.UpdateEventHandler, $"{ecr.Name.ToLower()}-domain-entity-handler-updated");
                eventBus.Subscribe(ecr.EventKeys.Delete, handlers.DeleteEventHandler, $"{ecr.Name.ToLower()}-domain-entity-handler-deleted");
            }
        }

        private static void InitializeServices(IServiceProvider serviceProvider)
        {
            ServiceResponseWrapperExtensions.Init(serviceProvider);
            GenericControllerNameConvention.Init(serviceProvider);
            MappingExtensions.Build(serviceProvider);
        }

        private static void AddPermissionComponents(IApplicationBuilder app, IServiceProvider serviceProvider, IEventBus eventBus, IEnumerable<EntityConfigRecord> ecrs)
        {
            var config = serviceProvider.GetRequiredService<AnyServiceConfig>();
            if (!config.ManageEntityPermissions)
                return;

            app.UseMiddleware<AnyServicePermissionMiddleware>();

            //subscribe to events event listener
            var ekr = ecrs.Select(e => e.EventKeys);
            var peh = serviceProvider.GetRequiredService<IPermissionEventsHandler>();
            foreach (var e in ekr)
            {
                eventBus.Subscribe(e.Create, peh.PermissionCreatedHandler, "entity-created-permission-handler");
                eventBus.Subscribe(e.Delete, peh.PermissionDeletedHandler, "entity-deleted-permission-handler");
            }
        }
    }
}
