using System;
using System.Collections.Generic;
using System.Linq;
using AnyService;
using AnyService.Core.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Utilities;
using AnyService.Services.Security;
using AnyService.Audity;
using AnyService.Events;

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
        public static IServiceCollection AddAnyService(this IServiceCollection services, AnyServiceConfig config)
        {
            NormalizeConfiguration(config);

            services.TryAddSingleton<IdGeneratorFactory>(sp =>
            {
                var stringGenerator = new StringIdGenerator();
                var f = new IdGeneratorFactory();
                f.AddOrReplace(typeof(string), stringGenerator);
                return f;
            });
            services.TryAddSingleton(config);

            services.TryAddTransient(typeof(CrudService<>));

            // services.
            services.AddSingleton(config.EntityConfigRecords);
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
            foreach (var ecr in config.EntityConfigRecords)
                services.TryAddScoped(ecr.FilterFactoryType);
            services.TryAddTransient(typeof(IFilterFactory), sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ffType = wc.CurrentEntityConfigRecord.FilterFactoryType;
                return sp.GetService(ffType) as IFilterFactory;
            });

            EntityConfigRecordManager.EntityConfigRecords = config.EntityConfigRecords;
            services.TryAddScoped<WorkContext>();
            services.TryAddSingleton<IIdGenerator, StringIdGenerator>();
            services.TryAddTransient<IPermissionManager, PermissionManager>();

            services.TryAddScoped(sp =>
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
            services.TryAddSingleton<IEventsBus, DefaultEventsBus>();

            if (config.ManageEntityPermissions)
                services.TryAddSingleton<IPermissionEventsHandler, DefaultPermissionsEventsHandler>();

            return services;
        }
        private static void NormalizeConfiguration(AnyServiceConfig config)
        {
            var temp = config.EntityConfigRecords.ToArray();
            foreach (var ecr in temp)
            {
                var e = ecr.Type;
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                if (!ecr.Route.HasValue()) ecr.Route = "/" + e.Name;
                if (!ecr.Route.StartsWith("/") || ecr.Route.StartsWith("//"))
                    throw new InvalidOperationException($"{nameof(EntityConfigRecord.Route)} must start with single'/'. Actual value: {ecr.Route}");

                var mapperType = ecr.ResponseMapperType;
                if (mapperType != null && !typeof(IServiceResponseMapper).IsAssignableFrom(mapperType))
                    throw new InvalidOperationException($"{nameof(EntityConfigRecord.ResponseMapperType)} must implement {nameof(IServiceResponseMapper)}");
                if (mapperType == null)
                    ecr.ResponseMapperType = typeof(DefaultServiceResponseMapper);

                if (ecr.EventKeys == null) ecr.EventKeys = ekr;
                if (ecr.PermissionRecord == null) ecr.PermissionRecord = pr;
                if (ecr.EntityKey == null) ecr.EntityKey = fn;
                if (ecr.PaginationSettings == null) ecr.PaginationSettings = config.DefaultPaginationSettings;
                if (ecr.FilterFactoryType == null) ecr.FilterFactoryType = config.FilterFactoryType;
                if (ecr.Validator == null)
                {
                    var v = typeof(AlwaysTrueCrudValidator<>).MakeGenericType(e);
                    ecr.Validator = (ICrudValidator)Activator.CreateInstance(v);
                }
                SetAuthorization(ecr.Authorization);

            }
            config.EntityConfigRecords = temp;

            // if (config.UseAuthorizationMiddleware && config.EntityConfigRecords.All(t => t.Authorization == null))
            //     config.UseAuthorizationMiddleware = false;
        }

        private static void SetAuthorization(AuthorizationInfo authzInfo)
        {
            if (authzInfo == null)
                return;

            var ctrlAuthzAttribute = authzInfo.ControllerAuthorizationNode;
            if (authzInfo.ControllerAuthorizationNode == null &&
                authzInfo.PostAuthorizationNode == null &&
                authzInfo.GetAuthorizationNode == null &&
                authzInfo.PutAuthorizationNode == null &&
                authzInfo.DeleteAuthorizationNode == null)
            {
                authzInfo = null;
                return;
            }


            if (ctrlAuthzAttribute == null && !ctrlAuthzAttribute.Roles.Any())
                ctrlAuthzAttribute = null;
            if (authzInfo.PostAuthorizationNode == null || !authzInfo.PostAuthorizationNode.Roles.Any()) authzInfo.PostAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.GetAuthorizationNode == null || !authzInfo.GetAuthorizationNode.Roles.Any()) authzInfo.GetAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.PutAuthorizationNode == null || !authzInfo.PutAuthorizationNode.Roles.Any()) authzInfo.PutAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.DeleteAuthorizationNode == null || !authzInfo.DeleteAuthorizationNode.Roles.Any()) authzInfo.DeleteAuthorizationNode = ctrlAuthzAttribute;
        }
    }
}
