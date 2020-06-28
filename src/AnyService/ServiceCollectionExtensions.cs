using System;
using System.Collections.Generic;
using System.Linq;
using AnyService;
using AnyService.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using AnyService.Utilities;
using AnyService.Services.Security;
using AnyService.Audity;
using AnyService.Events;
using AnyService.Endpoints;

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
            services.TryAddSingleton<IdGeneratorFactory>(sp =>
            {
                var stringGenerator = new StringIdGenerator();
                var f = new IdGeneratorFactory();
                f.AddOrReplace(typeof(string), stringGenerator);
                return f;
            });
            NormalizeConfiguration(config);

            services.TryAddSingleton(config);

            services.TryAddTransient(typeof(ICrudService<>), typeof(CrudService<>));

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
            {
                ValidateInjectedType<IFilterFactory>(ecr.FilterFactoryType);
                services.TryAddScoped(ecr.FilterFactoryType);

                var srv = typeof(IModelPreparar<>).MakeGenericType(ecr.Type);
                var impl = ecr.ModelPrepararType.IsGenericTypeDefinition ?
                    ecr.ModelPrepararType.MakeGenericType(ecr.Type) :
                    ecr.ModelPrepararType;
                services.TryAddTransient(srv, impl);
            }
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
            services.TryAddSingleton<IEventBus, DefaultEventsBus>();

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

                if (!ecr.Route.Value.HasValue()) ecr.Route = "/" + e.Name;

                ecr.Name ??= ecr.Type.Name;
                ecr.ResponseMapperType ??= config.ServiceResponseMapperType;
                ValidateInjectedType<IServiceResponseMapper>(ecr.ResponseMapperType);
                ecr.EventKeys ??= ekr;
                ecr.PermissionRecord ??= pr;
                ecr.EntityKey ??= fn;
                ecr.PaginationSettings ??= config.DefaultPaginationSettings;
                ecr.FilterFactoryType ??= config.FilterFactoryType;
                ecr.ModelPrepararType ??= config.ModelPrepararType;

                if (ecr.Validator == null)
                {
                    var v = typeof(AlwaysTrueCrudValidator<>).MakeGenericType(e);
                    ecr.Validator = (ICrudValidator)Activator.CreateInstance(v);
                }
                ecr.Authorization = SetAuthorization(ecr.Authorization);

            }
            config.EntityConfigRecords = temp;

            // if (config.UseAuthorizationMiddleware && config.EntityConfigRecords.All(t => t.Authorization == null))
            //     config.UseAuthorizationMiddleware = false;
        }

        private static void ValidateInjectedType<TService>(Type injectedType)
        {
            if (injectedType != null && !typeof(TService).IsAssignableFrom(injectedType))
                throw new InvalidOperationException($"{injectedType.Name} must implement {nameof(TService)}");
        }

        private static AuthorizationInfo SetAuthorization(AuthorizationInfo authzInfo)
        {
            if (authzInfo == null)
                return null;

            var ctrlAuthzAttribute = authzInfo.ControllerAuthorizationNode;
            if (authzInfo.ControllerAuthorizationNode == null &&
                authzInfo.PostAuthorizationNode == null &&
                authzInfo.GetAuthorizationNode == null &&
                authzInfo.PutAuthorizationNode == null &&
                authzInfo.DeleteAuthorizationNode == null)
                return null;

            //align authorization with controller's if empty or null
            if (ctrlAuthzAttribute == null && ctrlAuthzAttribute.Roles.IsNullOrEmpty())
                ctrlAuthzAttribute = null;
            if (authzInfo.PostAuthorizationNode == null || authzInfo.PostAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.PostAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.GetAuthorizationNode == null || authzInfo.GetAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.GetAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.PutAuthorizationNode == null || authzInfo.PutAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.PutAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.DeleteAuthorizationNode == null || authzInfo.DeleteAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.DeleteAuthorizationNode = ctrlAuthzAttribute;

            return authzInfo;
        }
    }
}
