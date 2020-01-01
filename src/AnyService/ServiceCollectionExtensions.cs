using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using AnyService.Audity;
using AnyService.Events;
using AnyService;
using AnyService.Core.Security;
using AnyService.Services.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IEnumerable<Type> entities)
        {
            var typeConfigRecords = entities.Select(e =>
            {
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                return new TypeConfigRecord
                {
                    Type = e,
                    RoutePrefix = "/" + e.Name,
                    EventKeyRecord = ekr,
                    PermissionRecord = pr,
                    EntityKey = fn,
                };
            });
            var config = new AnyServiceConfig
            {
                TypeConfigRecords = typeConfigRecords
            };
            return AddAnyService(services, config);
        }
        // public static IServiceCollection AddAnyService(this IServiceCollection services,
        //     IEnumerable<Type> entities,
        //     IEnumerable<ICrudValidator> validators)
        // {
        //     var typeConfigRecords = entities.Select(e =>
        //     {
        //         var fn = e.FullName.ToLower();
        //         var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
        //         var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

        //         return new TypeConfigRecord(e, "/" + e.Name, ekr, pr, fn);
        //     });
        //     return AddAnyService(services, typeConfigRecords, validators, null);
        // }

        public static IServiceCollection AddAnyService(this IServiceCollection services,
            AnyServiceConfig config)
        {
            services.TryAddSingleton(config ?? new AnyServiceConfig());
            // mvcBuilder.ConfigureApplicationPartManager(apm =>
            //     apm.FeatureProviders.Add(new GenericControllerFeatureProvider(typeConfigRecords.Select(e => e.Type))));
            services.TryAddTransient(typeof(CrudService<>));

            // services.
            services.AddSingleton(config.TypeConfigRecords);
            // var validatorFactory = new ValidatorFactory(validators);
            // services.TryAddSingleton(validatorFactory);
            // foreach (var v in validators)
            // {
            //     var vType = v.GetType();
            //     foreach (var vt in vType.GetInterfaces())
            //         services.TryAddTransient(vt, vType);
            // }

            TypeConfigRecordManager.TypeConfigRecords = config.TypeConfigRecords;
            services.TryAddScoped<WorkContext>();
            services.TryAddScoped<IPermissionManager, PermissionManager>();

            //services.AddScoped(sp =>
            //{
            //    var wc = sp.GetService<WorkContext>();
            //    var ct = wc.CurrentType;
            //    return TypeConfigRecordManager.GetRecord(ct).EventKeyRecord;
            //});
            //services.AddScoped(sp =>
            //{
            //    var wc = sp.GetService<WorkContext>();
            //    var ct = wc.CurrentType;
            //    return TypeConfigRecordManager.GetRecord(ct).PermissionRecord;
            //});

            services.TryAddScoped<AuditHelper>();
            services.TryAddSingleton<IEventBus, EventBus>();

            return services;
        }
    }
}
