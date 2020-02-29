using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Audity;
using AnyService.Events;
using AnyService;
using AnyService.Core.Security;
using AnyService.Services.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authorization;
using AnyService.Services;
using AnyService.Services.ResponseMappers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            IEnumerable<Type> entities)
        {
            var config = new AnyServiceConfig
            {
                EntityConfigRecords = entities.Select(e => new EntityConfigRecord { Type = e, })
            };
            return AddAnyService(services, config);
        }
        public static IServiceCollection AddAnyService(this IServiceCollection services,
            AnyServiceConfig config)
        {
            NormalizeConfiguration(config);
            services.TryAddSingleton(config);

            services.TryAddTransient(typeof(CrudService<>));

            // services.
            services.AddSingleton(config.EntityConfigRecords);
            if (config.EntityConfigRecords.Any(t => t.Authorization != null))
                services.AddTransient<IAuthorizationHandler, DefaultAuthorizationHandler>();
            //mappers
            var mappers = config.EntityConfigRecords.Select(t => t.ResponseMapperType).ToArray();
            foreach (var m in mappers)
                services.TryAddSingleton(m);

            //validator factory
            var validators = config.EntityConfigRecords.Select(t => t.Validator).ToArray();
            var validatorFactory = new ValidatorFactory(validators);
            services.TryAddSingleton(validatorFactory);
            foreach (var v in validators)
            {
                var vType = v.GetType();
                foreach (var vt in vType.GetInterfaces())
                    services.TryAddTransient(vt, vType);
            }

            EntityConfigRecordManager.EntityConfigRecords = config.EntityConfigRecords;
            services.TryAddScoped<WorkContext>();
            services.TryAddTransient<IPermissionManager, PermissionManager>();

            services.AddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                return EntityConfigRecordManager.GetRecord(ct).EventKeys;
            });
            services.AddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                return EntityConfigRecordManager.GetRecord(ct).PermissionRecord;
            });

            services.AddTransient(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var mt = wc.CurrentEntityConfigRecord.ResponseMapperType;
                return sp.GetService(mt) as IServiceResponseMapper;
            });

            services.TryAddScoped<AuditHelper>();
            services.TryAddSingleton<IDomainEventsBus, DomainEventsBus>();

            if (config.ManageEntityPermissions)
                services.TryAddSingleton<IPermissionEventsHandler, DefaultPermissionsEventsHandler>();

            return services;
        }

        private static void NormalizeConfiguration(AnyServiceConfig config)
        {
            var temp = config.EntityConfigRecords.ToArray();
            foreach (var tcr in temp)
            {
                var e = tcr.Type;
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                if (!tcr.Route.HasValue()) tcr.Route = "/" + e.Name;
                if (!tcr.Route.StartsWith("/") || tcr.Route.StartsWith("//"))
                    throw new InvalidOperationException($"{nameof(EntityConfigRecord.Route)} must start with single'/'. Actual value: {tcr.Route}");

                var mapperType = tcr.ResponseMapperType;
                if (mapperType != null && !typeof(IServiceResponseMapper).IsAssignableFrom(mapperType))
                    throw new InvalidOperationException($"{nameof(EntityConfigRecord.ResponseMapperType)} must implement {nameof(IServiceResponseMapper)}");
                if (mapperType == null)
                    tcr.ResponseMapperType = typeof(DefaultServiceResponseMapper);

                if (tcr.EventKeys == null) tcr.EventKeys = ekr;
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
            config.EntityConfigRecords = temp;
        }

        private static void SetAuthorization(AuthorizationInfo authzInfo)
        {
            var ctrlAuthzAttribute = authzInfo.ControllerAuthorizationNode;

            if (authzInfo.PostAuthorizeNode == null) authzInfo.PostAuthorizeNode = ctrlAuthzAttribute;
            if (authzInfo.GetAuthorizeNode == null) authzInfo.GetAuthorizeNode = ctrlAuthzAttribute;
            if (authzInfo.PutAuthorizeNode == null) authzInfo.PutAuthorizeNode = ctrlAuthzAttribute;
            if (authzInfo.DeleteAuthorizeNode == null) authzInfo.DeleteAuthorizeNode = ctrlAuthzAttribute;
        }
    }
}
