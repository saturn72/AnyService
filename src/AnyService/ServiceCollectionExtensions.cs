using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Services;
using AnyService.Audity;
using AnyService.Events;
using AnyService;
using AnyService.Controllers;
using AnyService.Core.Security;
using AnyService.Services.Security;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IMvcBuilder mvcBuilder,
            IEnumerable<Type> entities)
        {
            var typeConfigRecords = entities.Select(e =>
            {
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                return new TypeConfigRecord(e, "/" + e.Name, ekr, pr, fn);
            });
            return AddAnyService(services, mvcBuilder, typeConfigRecords, null);
        }
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IMvcBuilder mvcBuilder,
            IEnumerable<Type> entities,
            IEnumerable<ICrudValidator> validators)
        {
            var typeConfigRecords = entities.Select(e =>
            {
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                return new TypeConfigRecord(e, "/" + e.Name, ekr, pr, fn);
            });
            return AddAnyService(services, mvcBuilder, typeConfigRecords, validators);
        }

        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IMvcBuilder mvcBuilder,
            IEnumerable<TypeConfigRecord> typeConfigRecords,
            IEnumerable<ICrudValidator> validators)
        {
            mvcBuilder.ConfigureApplicationPartManager(apm =>
                apm.FeatureProviders.Add(new GenericControllerFeatureProvider(typeConfigRecords.Select(e => e.Type))));
            services.AddTransient(typeof(CrudService<>));

            var validatorFactory = new ValidatorFactory(validators);
            services.AddSingleton(validatorFactory);
            foreach (var v in validators)
            {
                var vType = v.GetType();
                foreach (var vt in vType.GetInterfaces())
                    services.AddTransient(vt, vType);
            }

            TypeConfigRecordManager.TypeConfigRecords = typeConfigRecords;
            services.AddScoped<WorkContext>();
            services.AddScoped<IPermissionManager, PermissionManager>();

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

            services.AddScoped<AuditHelper>();
            services.AddSingleton<IEventBus, EventBus>();

            return services;
        }
    }
}
