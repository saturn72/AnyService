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
using AnyService.Events;
using Microsoft.AspNetCore.Http;
using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc;
using AnyService.Services.Audit;
using AnyService.Services.Preparars;
using AnyService.Audity;

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
            services.TryAddTransient(sp => sp.GetService<WorkContext>().CurrentEntityConfigRecord?.AuditSettings ?? config.AuditSettings);

            services.TryAddTransient(typeof(ICrudService<>), typeof(CrudService<>));

            // services.
            services.AddSingleton(config.EntityConfigRecords);
            //mappers
            var mappers = config.EntityConfigRecords.Select(t => t.ControllerSettings.ResponseMapperType).ToArray();
            foreach (var m in mappers)
                services.TryAddSingleton(m);

            //validator factory
            var validatorTypes = config.EntityConfigRecords.Select(t => t.CrudValidatorType).ToArray();
            foreach (var vType in validatorTypes)
            {
                foreach (var vt in vType.GetAllBaseTypes(typeof(object)))
                    services.TryAddTransient(vt, vType);
            }
            foreach (var ecr in config.EntityConfigRecords)
            {
                ValidateType<IFilterFactory>(ecr.FilterFactoryType);
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

            services.TryAddScoped<WorkContext>();
            services.TryAddSingleton<IIdGenerator, StringIdGenerator>();
            services.TryAddTransient<IPermissionManager, PermissionManager>();

            services.TryAddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                //var ecrm = sp.GetService<EntityConfigRecordManager>();
                return wc.CurrentEntityConfigRecord.EventKeys;
            });
            services.AddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ct = wc.CurrentType;
                return wc.CurrentEntityConfigRecord.PermissionRecord;
            });

            services.AddTransient(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var mt = wc.CurrentEntityConfigRecord?.ControllerSettings.ResponseMapperType ?? config.ServiceResponseMapperType;
                return sp.GetService(mt) as IServiceResponseMapper;
            });

            var auditManagerType = config.AuditSettings.Active ?
                typeof(AuditManager) :
                typeof(DummyAuditManager);

            services.TryAddScoped(typeof(IAuditManager), auditManagerType);
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

                ecr.Name ??= ecr.Type.Name;

                var hasDuplication = temp.Where(e => e.Name == ecr.Name);
                if (hasDuplication.Count() > 1)
                    throw new InvalidOperationException($"Duplication in {nameof(EntityConfigRecord.Name)} field : {ecr.Name}. Please provide unique name for the controller. See configured entities where Routes equals {hasDuplication.First().ControllerSettings.Route} and {hasDuplication.Last().ControllerSettings.Route}");

                ecr.EventKeys ??= ekr;
                ecr.PermissionRecord ??= pr;
                ecr.EntityKey ??= fn;
                ecr.PaginationSettings ??= config.DefaultPaginationSettings;
                ecr.FilterFactoryType ??= config.FilterFactoryType;
                ecr.ModelPrepararType ??= config.ModelPrepararType;

                ecr.AuditSettings = NormalizeAudity(ecr, config.AuditSettings);
                ecr.ControllerSettings = NormalizeControllerSettings(ecr, config);

                if (ecr.CrudValidatorType != null)
                {
                    var cvType = typeof(CrudValidatorBase<>);
                    //validate inheritance from CrudValidatorBase<>
                    if (!ecr.CrudValidatorType.GetAllBaseTypes().All(t => t != cvType))
                        throw new InvalidOperationException($"{ecr.CrudValidatorType.Name} must implement {typeof(CrudValidatorBase<>).Name}");
                }
                else
                {
                    ecr.CrudValidatorType = typeof(AlwaysTrueCrudValidator<>).MakeGenericType(e);
                }
            }
            config.EntityConfigRecords = temp;
        }

        private static ControllerSettings NormalizeControllerSettings(EntityConfigRecord ecr, AnyServiceConfig config)
        {
            var settings = ecr.ControllerSettings;
            if (settings == null)
                settings = new ControllerSettings();
            if (!settings.Route.HasValue) settings.Route = new PathString("/" + ecr.Type.Name);

            var route = settings.Route;
            if (route.Value.EndsWith("/"))
                settings.Route = new PathString(route.Value[0..^1]);

            settings.ResponseMapperType ??= config.ServiceResponseMapperType;
            ValidateType<IServiceResponseMapper>(settings.ResponseMapperType);

            settings.ControllerType ??= typeof(GenericController<>).MakeGenericType(ecr.Type);
            ValidateType<ControllerBase>(settings.ControllerType);

            settings.Authorization = SetAuthorization(settings.Authorization);

            return settings;
        }
        private static AuditSettings NormalizeAudity(EntityConfigRecord ecr, AuditSettings serverAuditSettings)
        {
            if (!serverAuditSettings.Active)
                return new AuditSettings
                {
                    AuditRules = new AuditRules()
                };

            return ecr.AuditRules == null ?
                serverAuditSettings :
                new AuditSettings
                {
                    Active = serverAuditSettings.Active,
                    AuditRules = ecr.AuditRules,
                };
        }

        private static void ValidateType<TService>(Type type)
        {
            if (type != null && !typeof(TService).IsAssignableFrom(type))
                throw new InvalidOperationException($"{type.Name} must implement {nameof(TService)}");
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
            if (ctrlAuthzAttribute == null || ctrlAuthzAttribute.Roles.IsNullOrEmpty())
                ctrlAuthzAttribute = null;
            if (authzInfo.PostAuthorizationNode == null || authzInfo.PostAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.PostAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.GetAuthorizationNode == null || authzInfo.GetAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.GetAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.PutAuthorizationNode == null || authzInfo.PutAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.PutAuthorizationNode = ctrlAuthzAttribute;
            if (authzInfo.DeleteAuthorizationNode == null || authzInfo.DeleteAuthorizationNode.Roles.IsNullOrEmpty()) authzInfo.DeleteAuthorizationNode = ctrlAuthzAttribute;

            return authzInfo;
        }
    }
}
