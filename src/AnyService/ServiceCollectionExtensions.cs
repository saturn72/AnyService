using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using AnyService.Services.Logging;
using AnyService.Models;
using AnyService.Logging;
using AnyService.ComponentModel;
using AnyService.Services.Internals;
using System.Collections;
using AnyService.Services.EntityMapping;

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
            var d = config.EntityConfigRecords
                .Select(s => s.Type.FullName)
                .GroupBy(x => x)
                .Where(g => g.Count() > 1);
            if (d.Any())
                throw new InvalidOperationException($"Multiple entity configurations for same type. see types: {d.ToJsonString()}");
            NormalizeConfiguration(config);
            RegisterDependencies(services, config);
            AddEntityConfigRecordsMappings(config.EntityConfigRecords);

            return services;
        }

        private static void AddEntityConfigRecordsMappings(IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            MappingExtensions.AddConfiguration(cfg =>
              {
                  foreach (var ecr in entityConfigRecords)
                  {
                      foreach (var es in ecr.EndpointSettings)
                      {
                          var mtt = es.MapToType;
                          if (mtt != ecr.Type)
                          {
                              cfg.CreateMap(ecr.Type, mtt);
                              cfg.CreateMap(mtt, ecr.Type);
                          }
                          var mtptType = es.MapToPaginationType;
                          var pType = typeof(Pagination<>).MakeGenericType(mtt);
                          if (mtptType != pType || mtptType != typeof(PaginationModel<>).MakeGenericType(mtt))
                          {
                              cfg.CreateMap(pType, mtptType);
                              cfg.CreateMap(mtptType, pType);
                          }
                      }
                  }
              });
        }

        private static void RegisterDependencies(IServiceCollection services, AnyServiceConfig config)
        {
            services.TryAddSingleton<IdGeneratorFactory>(sp =>
            {
                var stringGenerator = new StringIdGenerator();
                var f = new IdGeneratorFactory();
                f.AddOrReplace(typeof(string), stringGenerator);
                return f;
            });

            services.TryAddSingleton(config);
            services.TryAddScoped(sp => sp.GetService<WorkContext>().CurrentEntityConfigRecord?.AuditSettings ?? config.AuditSettings);
            services.TryAddScoped(typeof(ICrudService<>), typeof(CrudService<>));

            services.AddSingleton(config.EntityConfigRecords);
            services.AddSingleton(config.EntityConfigRecords.SelectMany(e => e.EndpointSettings));
            //response mappers
            var mappers = config.EntityConfigRecords.SelectMany(t => t.EndpointSettings.Select(es => es.ResponseMapperType));
            foreach (var m in mappers)
                services.TryAddSingleton(m);

            //validator factory
            var validatorTypes = config.EntityConfigRecords.Select(t => t.CrudValidatorType).ToArray();
            foreach (var vType in validatorTypes)
            {
                foreach (var vt in vType.GetAllBaseTypes(typeof(object)))
                    services.TryAddScoped(vt, vType);
            }
            foreach (var ecr in config.EntityConfigRecords)
            {
                ValidateType<IFilterFactory>(ecr.FilterFactoryType);
                services.TryAddScoped(ecr.FilterFactoryType);

                var srv = typeof(IModelPreparar<>).MakeGenericType(ecr.Type);
                var impl = ecr.ModelPrepararType.IsGenericTypeDefinition ?
                    ecr.ModelPrepararType.MakeGenericType(ecr.Type) :
                    ecr.ModelPrepararType;
                services.TryAddScoped(srv, impl);
            }
            services.TryAddScoped(typeof(IFilterFactory), sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var ffType = wc.CurrentEntityConfigRecord.FilterFactoryType;
                return sp.GetService(ffType) as IFilterFactory;
            });

            services.TryAddScoped<WorkContext>();
            services.TryAddSingleton<IIdGenerator, StringIdGenerator>();
            services.TryAddTransient<IPermissionManager, PermissionManager>();
            services.TryAddTransient<ILogRecordManager, LogRecordManager>();

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

            services.TryAddScoped(sp =>
            {
                var wc = sp.GetService<WorkContext>();
                var mt = wc.CurrentEndpointSettings?.ResponseMapperType ?? config.ServiceResponseMapperType;
                return sp.GetService(mt) as IServiceResponseMapper;
            });

            var auditManagerType = config.AuditSettings.Active ?
                typeof(AuditManager) :
                typeof(DummyAuditManager);

            services.TryAddTransient(typeof(IAuditManager), auditManagerType);
            services.TryAddSingleton<IEventBus, DefaultEventsBus>();

            if (config.ManageEntityPermissions)
                services.TryAddSingleton<IPermissionEventsHandler, DefaultPermissionsEventsHandler>();
        }
        private static void NormalizeConfiguration(AnyServiceConfig config)
        {
            AddAnyServiceControllers(config);
            var temp = config.EntityConfigRecords.ToArray();

            foreach (var ecr in temp)
            {
                var e = ecr.Type;
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var pr = new PermissionRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");

                ecr.Name ??= e.Name;
                ecr.ExternalName ??= e.Name;
                ecr.EventKeys ??= ekr;
                ecr.PermissionRecord ??= pr;
                ecr.EntityKey ??= fn;
                ecr.PaginationSettings ??= config.DefaultPaginationSettings;
                ecr.FilterFactoryType ??= config.FilterFactoryType;
                ecr.ModelPrepararType ??= config.ModelPrepararType;

                ecr.AuditSettings = NormalizeAudity(ecr, config.AuditSettings);
                ecr.EndpointSettings = NormalizeEndpointSettings(ecr, config);
                ecr.EntityMappingSettings ??= new EntityMappingSettings();

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

            //check invlaid records configurations
            var duplicatedNames = temp.GroupBy(x => x.Name).Where(g => g.Skip(1).Any()).Select(d => d);
            if (!duplicatedNames.IsNullOrEmpty())
                throw new InvalidOperationException($"Multiple names of same entity. Please remove duplication of {duplicatedNames.ToJsonString()}");
            config.EntityConfigRecords = temp;
        }
        private static void AddAnyServiceControllers(AnyServiceConfig config)
        {
            var list = new List<EntityConfigRecord>(config.EntityConfigRecords);
            if (config.AuditSettings.Active)
            {
                list.Add(new EntityConfigRecord
                {
                    Type = typeof(AuditRecord),
                    EndpointSettings = new[]
                    {
                        new EndpointSettings
                        {
                            Area = "__anyservice",
                            ControllerType = typeof(AuditController),
                        },
                    }
                });
            }
            if (config.UseLogRecordEndpoint)
            {
                list.Add(new EntityConfigRecord
                {
                    Type = typeof(LogRecord),
                    EndpointSettings = new[]
                    {
                        new EndpointSettings
                    {
                        Area = "__anyservice",
                        ControllerType = typeof(LogRecordController),
                    },}
                });
            }
            config.EntityConfigRecords = list;
        }

        private static IEnumerable<EndpointSettings> NormalizeEndpointSettings(EntityConfigRecord ecr, AnyServiceConfig config)
        {
            //set empty settings for nulls
            if (ecr.EndpointSettings == null)
                ecr.EndpointSettings = new[] { new EndpointSettings() };

            //return if all disabled
            if (ecr.EndpointSettings.All(e => e.Disabled))
                return new EndpointSettings[] { };

            var activeSettings = ecr.EndpointSettings.Where(e => !e.Disabled);
            //duplication on area and route
            var hasDuplicates =
                activeSettings.Where(x => x.Area.HasValue()).GroupBy(i => i.Area).Any(g => g.Count() > 1) ||
                activeSettings.Where(x => x.Route.HasValue || x.Name.HasValue()).GroupBy(i => $"{i.Route}_{i.Name}").Any(g => g.Count() > 1);

            if (hasDuplicates)
                throw new InvalidOperationException($"Duplication in {nameof(EntityConfigRecord.EndpointSettings)}. Duplicated field may be {ecr.Name} or {nameof(EndpointSettings.Area)} and {nameof(EndpointSettings.Route)} combination.");

            var res = new List<EndpointSettings>();
            foreach (var settings in activeSettings)
            {
                BuildControllerMethodSettings(settings, ecr);

                if (!settings.Route.HasValue)
                {
                    var areaPrefix = settings.Area.HasValue() ? $"{settings.Area}/" : "";
                    settings.Route = new PathString($"/{areaPrefix}{ecr.Type.Name}");
                }

                var route = settings.Route;
                if (route.Value.EndsWith("/"))
                    settings.Route = new PathString(route.Value[0..^1]);

                settings.ResponseMapperType ??= config.ServiceResponseMapperType;
                ValidateType<IServiceResponseMapper>(settings.ResponseMapperType);
                ValidateType<ControllerBase>(settings.ControllerType);
                SetAuthorization(settings);

                settings.MapToType ??= ecr.Type;
                settings.MapToPaginationType ??= typeof(PaginationModel<>).MakeGenericType(settings.MapToType);

                settings.ControllerType ??= BuildController(ecr.Type, settings);

                settings.Name ??= settings.Area.HasValue() ?
                   $"{settings.Area}_{ecr.Type.Name}" :
                   settings.Route.Value.Replace("/", "_")[1..];

                settings.EntityConfigRecord = ecr;
                res.Add(settings);
            }
            return res;
        }

        private static void BuildControllerMethodSettings(EndpointSettings settings, EntityConfigRecord ecr)
        {
            var defaultControllerMethodSettings = new EndpointMethodSettings { Active = true };
            settings.PostSettings ??= defaultControllerMethodSettings;
            settings.GetSettings ??= defaultControllerMethodSettings;
            settings.PutSettings ??= defaultControllerMethodSettings;
            settings.DeleteSettings ??= defaultControllerMethodSettings;

            if (
                !settings.PostSettings.Active &&
                !settings.GetSettings.Active &&
                !settings.PutSettings.Active &&
                !settings.DeleteSettings.Active)
                throw new ArgumentException($"Invalid operation: {nameof(EntityConfigRecord)} named {ecr.Name} has all httpMethods deactivated");
        }

        private static Type BuildController(Type entityType, EndpointSettings settings)
        {
            var mapToType = settings.MapToType;
            return typeof(GenericController<,>).MakeGenericType(mapToType, entityType);
        }
        private static AuditSettings NormalizeAudity(EntityConfigRecord ecr, AuditSettings auditSettings)
        {
            if (!auditSettings.Active)
                return new AuditSettings
                {
                    AuditRules = new AuditRules()
                };

            return ecr.AuditRules == null ?
                auditSettings :
                new AuditSettings
                {
                    Active = auditSettings.Active,
                    AuditRules = ecr.AuditRules,
                };
        }

        private static void ValidateType<TService>(Type type)
        {
            if (type != null && !typeof(TService).IsAssignableFrom(type))
                throw new InvalidOperationException($"{type.Name} must implement {nameof(TService)}");
        }
        private static void SetAuthorization(EndpointSettings settings)
        {
            var ctrlAuthzAttribute = settings.Authorization;

            settings.PostSettings.Authorization ??= ctrlAuthzAttribute;
            settings.GetSettings.Authorization ??= ctrlAuthzAttribute;
            settings.PutSettings.Authorization ??= ctrlAuthzAttribute;
            settings.DeleteSettings.Authorization ??= ctrlAuthzAttribute;
        }
    }
}
