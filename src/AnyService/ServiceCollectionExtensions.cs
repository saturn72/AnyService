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
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IEnumerable<Type> entities)
        {
            var config = new AnyServiceConfig
            {
                TypeConfigRecords = entities.Select(e => new TypeConfigRecord { Type = e, })
            };
            return AddAnyService(services, config);
        }
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            AnyServiceConfig config)
        {
            NormalizeConfiguration(config);
            services.TryAddSingleton(config);
            // mvcBuilder.ConfigureApplicationPartManager(apm =>
            //     apm.FeatureProviders.Add(new GenericControllerFeatureProvider(typeConfigRecords.Select(e => e.Type))));
            services.TryAddTransient(typeof(CrudService<>));

            // services.
            services.AddSingleton(config.TypeConfigRecords);
            if (config.TypeConfigRecords.Any(t => t.Authorization != null))
                services.AddTransient<IAuthorizationHandler, DefaultAuthorizationHandler>();

            //validator factory
            var validators = config.TypeConfigRecords.Select(t => t.Validator).ToArray();
            var validatorFactory = new ValidatorFactory(validators);
            services.TryAddSingleton(validatorFactory);
            foreach (var v in validators)
            {
                var vType = v.GetType();
                foreach (var vt in vType.GetInterfaces())
                    services.TryAddTransient(vt, vType);
            }

            TypeConfigRecordManager.TypeConfigRecords = config.TypeConfigRecords;
            services.TryAddScoped<WorkContext>();
            services.TryAddScoped<IPermissionManager, PermissionManager>();

            services.AddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                return TypeConfigRecordManager.GetRecord(ct).EventKeyRecord;
            });
            services.AddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                return TypeConfigRecordManager.GetRecord(ct).PermissionRecord;
            });

            services.TryAddScoped<AuditHelper>();
            services.TryAddSingleton<IEventBus, EventBus>();

            return services;
        }

        private static void NormalizeConfiguration(AnyServiceConfig config)
        {
            var temp = config.TypeConfigRecords.ToArray();
            foreach (var tcr in temp)
            {
                var e = tcr.Type;
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                if (!tcr.RoutePrefix.HasValue()) tcr.RoutePrefix = "/" + e.Name;
                if (!tcr.RoutePrefix.StartsWith("/") || tcr.RoutePrefix.StartsWith("//"))
                    throw new InvalidOperationException($"RoutePrefix must start with single'/'. Actual value: {tcr.RoutePrefix}");

                if (tcr.EventKeyRecord == null) tcr.EventKeyRecord = ekr;
                if (tcr.PermissionRecord == null) tcr.PermissionRecord = pr;
                if (tcr.EntityKey == null) tcr.EntityKey = fn;
                if (tcr.Validator == null)
                {
                    var v = typeof(AlwaysTrueCrudValidator<>).MakeGenericType(e);
                    tcr.Validator = (ICrudValidator)Activator.CreateInstance(v);
                }
                if (tcr.Authorization != null)
                    SetAuthorization(tcr.Authorization);
            }
            config.TypeConfigRecords = temp;
        }

        private static void SetAuthorization(AuthorizationInfo authzInfo)
        {
            var ctrlAuthzAttribute = authzInfo.ControllerAuthorizeAttribute;

            if (authzInfo.PostAuthorizeAttribute == null) authzInfo.PostAuthorizeAttribute = ctrlAuthzAttribute;
            if (authzInfo.GetAuthorizeAttribute == null) authzInfo.GetAuthorizeAttribute = ctrlAuthzAttribute;
            if (authzInfo.PutAuthorizeAttribute == null) authzInfo.PutAuthorizeAttribute = ctrlAuthzAttribute;
            if (authzInfo.DeleteAuthorizeAttribute == null) authzInfo.DeleteAuthorizeAttribute = ctrlAuthzAttribute;
        }
    }
}
