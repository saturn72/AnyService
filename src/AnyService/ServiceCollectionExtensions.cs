using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using AnyService.Audity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Controllers;
using AnyService.Events;

namespace AnyService
{
    public static class ServiceCollectionExtensions
    {
        private const string ModelSuffix = "model";
        public static IServiceCollection AddAnyService(this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<Type> entities,
        IEnumerable<ICrudValidator> validators)
        {
            var typeConfigRecords = entities.ToDictionary(k => k, v =>
            {
                var fn = v.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var route = v.Name;
                if (route.EndsWith(ModelSuffix, StringComparison.InvariantCultureIgnoreCase) && route != ModelSuffix)
                    route = route.Substring(0, route.LastIndexOf(ModelSuffix, StringComparison.InvariantCultureIgnoreCase));

                return new TypeConfigRecord(v, "/" + route, v.FullName, ekr);
            });
            return AddAnyService(services, configuration, typeConfigRecords, validators);
        }
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IConfiguration configuration,
            IReadOnlyDictionary<Type, TypeConfigRecord> typeConfigRecords,
            IEnumerable<ICrudValidator> validators)
        {
            var crudServiceType = typeof(CrudService<>);
            var createMethodInfo = crudServiceType.GetMethod("Create");
            services.AddTransient(crudServiceType);

            services.AddTransient<CrudController>(sp =>
            {
                var wc = sp.GetService<AnyServiceWorkContext>();

                var genericType = crudServiceType.MakeGenericType(wc.CurrentType);
                var srv = sp.GetService(genericType);

                return new CrudController(srv, wc);
            });
            var validatorFactory = new ValidatorFactory(validators);
            services.AddSingleton(validatorFactory);
            foreach (var v in validators)
            {
                var vType = v.GetType();
                foreach (var vt in vType.GetInterfaces())
                    services.AddTransient(vt, vType);
            }

            var routeMapper = new AnyServiceRouteMapper(typeConfigRecords);
            services.AddSingleton<AnyServiceRouteMapper>(routeMapper);
            services.AddScoped<AnyServiceWorkContext>();
            services.AddScoped<EventKeyRecord>(sp =>
            {
                var ek = sp.GetService<EventKeys>();
                var wc = sp.GetService<AnyServiceWorkContext>();
                return ek[wc.CurrentType];
            });


            var eventKeys = new EventKeys(typeConfigRecords.Select(tcr => tcr.Value));
            services.AddSingleton<EventKeys>(c => eventKeys);
            services.AddScoped<AuditHelper>();
            services.AddSingleton<IEventBus, EventBus>();

            return services;
        }
    }
}
