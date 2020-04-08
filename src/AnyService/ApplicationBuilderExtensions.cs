using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Controllers;
using AnyService.Core.Caching;
using AnyService.Events;
using AnyService.Middlewares;
using AnyService.Services.Security;
using AnyService.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnyService
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAnyService(this IApplicationBuilder app)
        {
            var sp = app.ApplicationServices;

            ValidateCoreServicesConfigured(sp);

            var apm = sp.GetService<ApplicationPartManager>();
            var typeConfigRecords = sp.GetService<IEnumerable<EntityConfigRecord>>();
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider());

            app.UseMiddleware<WorkContextMiddleware>();
            var options = sp.GetService<AnyServiceConfig>();
            if (options.UseAuthorizationMiddleware)
                app.UseMiddleware<DefaultAuthorizationMiddleware>();
            AddPermissionComponents(app, sp);
            return app;
        }
        private static void ValidateCoreServicesConfigured(IServiceProvider serviceProvider)
        {
            ExceptionsLogger.Init(
                serviceProvider.GetService<ILogger<ExceptionsLogger>>(),
                serviceProvider.GetService<IdGeneratorFactory>().GetGenerator(typeof(string)));

            if (!ExceptionsLogger.WasInit)
                throw new InvalidOperationException($"{nameof(ExceptionsLogger)} was not configured");
            ThrowIfNotConfigured<ICacheManager>();

            void ThrowIfNotConfigured<TService>()
            {
                if (serviceProvider.GetService<TService>() == null)
                    throw new InvalidOperationException($"{nameof(TService)} is not configured");
            }
        }
        private static void AddPermissionComponents(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var config = serviceProvider.GetService<AnyServiceConfig>();
            if (!config.ManageEntityPermissions)
                return;

            app.UseMiddleware<AnyServicePermissionMiddleware>();

            //subscribe to events event listener
            var eventBus = serviceProvider.GetService<IDomainEventsBus>();
            var ekr = EntityConfigRecordManager.EntityConfigRecords.Select(e => e.EventKeys);
            var peh = serviceProvider.GetService<IPermissionEventsHandler>();
            foreach (var e in ekr)
            {
                eventBus.Subscribe(e.Create, peh.EntityCreatedHandler);
                eventBus.Subscribe(e.Delete, peh.EntityDeletedHandler);
            }
        }
    }
}
