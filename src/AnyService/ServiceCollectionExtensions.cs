using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using AnyService.Audity;
using Microsoft.Extensions.Configuration;
using AnyService.Events;
using AnyService;
using AnyService.Controllers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IMvcBuilder mvcBuilder,
            IConfiguration configuration,
            IEnumerable<Type> entities,
            IEnumerable<ICrudValidator> validators)
        {
            var typeConfigRecords = entities.Select(e =>
            {
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                return new TypeConfigRecord(e, "/" + e.Name, ekr);
            });
            return AddAnyService(services, mvcBuilder, configuration, typeConfigRecords, validators);
        }

        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IMvcBuilder mvcBuilder,
            IConfiguration configuration,
            IEnumerable<TypeConfigRecord> typeConfigRecords,
            IEnumerable<ICrudValidator> validators)
        {
            mvcBuilder.ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new GenericControllerFeatureProvider(typeConfigRecords.Select(e =>e.Type))));
            services.AddTransient(typeof(CrudService<>));

            var anyServiceConfig = new AnyServiceConfig();
            configuration.GetSection("anyservice").Bind(anyServiceConfig);
            services.AddSingleton(anyServiceConfig);
            var validatorFactory = new ValidatorFactory(validators);
            services.AddSingleton(validatorFactory);
            foreach (var v in validators)
            {
                var vType = v.GetType();
                foreach (var vt in vType.GetInterfaces())
                    services.AddTransient(vt, vType);
            }
            
            RouteMapper.TypeConfigRecords = typeConfigRecords;
            services.AddScoped<WorkContext>();
            services.AddScoped(sp =>
            {
                var ek = sp.GetService<EventKeys>();
                var wc = sp.GetService<WorkContext>();
                return ek[wc.CurrentType];
            });

            var eventKeys = new EventKeys(typeConfigRecords);
            services.AddSingleton(c => eventKeys);
            services.AddScoped<AuditHelper>();
            services.AddSingleton<IEventBus, EventBus>();

            return services;
        }
    }
}
