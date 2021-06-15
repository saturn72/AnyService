using System;
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
using AnyService.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AnyService
{
    public static class ApplicationBuilderExtensions
    {
        public static void LogAnyServiceEndpoints<TStartup>(
            this IApplicationBuilder app,
            LogLevel logLevel = LogLevel.Debug)
        {
            var sb = new StringBuilder();
            var ecrs = app.ApplicationServices.GetService<IEnumerable<EntityConfigRecord>>();
            sb.AppendLine("List of active generic controllers endpoints:");
            foreach (var e in ecrs)
                sb.AppendLine(e.EndpointSettings.Route);

            var log = app.ApplicationServices.GetService<ILogger<TStartup>>();
            log.Log(logLevel, sb.ToString());
        }
        public static IApplicationBuilder UseAnyService(
            this IApplicationBuilder app,
            bool useWorkContextMiddleware = true,
            bool useAuthorizationMiddleware = true,
            bool logExceptions = true,
            bool usePermissionMiddleware = true)
        {
            var appServices = app.ApplicationServices;

            using var scope = appServices.CreateScope();
            var apm = scope.ServiceProvider.GetRequiredService<ApplicationPartManager>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider(appServices));

            InitializeServices(appServices);
            NormlizeProjectionMaps(scope.ServiceProvider);
            var cm = scope.ServiceProvider.GetRequiredService<ICacheManager>();
            var entityConfigRecords = scope.ServiceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();
            var eventBus = appServices.GetRequiredService<IDomainEventBus>();

            if (useWorkContextMiddleware) app.UseMiddleware<WorkContextMiddleware>();
            if (useAuthorizationMiddleware) app.UseMiddleware<DefaultAuthorizationMiddleware>();

            if (logExceptions)
                SubscribeLogExceptionHandler(eventBus, entityConfigRecords);
            SubscribeAuditHandler(appServices, eventBus, entityConfigRecords);

            if (usePermissionMiddleware)
                AddPermissionComponents(app, appServices, eventBus, entityConfigRecords);
            return app;
        }

        private static void NormlizeProjectionMaps(IServiceProvider serviceProvider)
        {
            var ecrs = serviceProvider.GetServices<EntityConfigRecord>();
            var mFactory = serviceProvider.GetService<IMapperFactory>();
            var mapper = mFactory.GetMapper(serviceProvider.GetService<AnyServiceConfig>().MapperName);
            foreach (var ecr in ecrs)
            {
                if (ecr.Type == ecr.EndpointSettings.MapToType)
                {
                    ecr.EndpointSettings.PropertiesProjectionMap = ecr.Type.GetProperties()
                        .Select(pi => pi.Name)
                        .ToDictionary(k => k, v => v, StringComparer.InvariantCultureIgnoreCase);
                    continue;
                }
                var m = mapper.ConfigurationProvider.GetAllTypeMaps()
                    .FirstOrDefault(x => x.SourceType == ecr.Type && x.DestinationType == ecr.EndpointSettings.MapToType);
                var projMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var pm in m.PropertyMaps)
                    projMap[pm.SourceMember.Name] = pm.DestinationName;

                ecr.EndpointSettings.PropertiesProjectionMap = projMap;
            }
        }
        private static void SubscribeAuditHandler(IServiceProvider sp, IDomainEventBus eventBus, IEnumerable<EntityConfigRecord> entityConfigRecords)
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

        private static void SubscribeLogExceptionHandler(
            IDomainEventBus eventBus,
            IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            var handlers = new ExceptionsLoggingEventHandlers();
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

        private static void AddPermissionComponents(IApplicationBuilder app, IServiceProvider serviceProvider, IDomainEventBus eventBus, IEnumerable<EntityConfigRecord> ecrs)
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
