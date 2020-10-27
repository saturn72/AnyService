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
using AnyService.Services.Logging;
using AnyService.Models;
using AnyService.Logging;
using AnyService.Infrastructure;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            RegisterDependencies(services, config);

            AddDefaultMapping(config.MapperName);
            AddEntityConfigRecordsMappings(config.MapperName, config.EntityConfigRecords);

            return services;
        }
        private static void AddDefaultMapping(string mapperName)
        {
            MappingExtensions.AddConfiguration(
                mapperName,
                cfg =>
                {
                    cfg.CreateMap(typeof(Pagination<>), typeof(PaginationModel<>))
                            .ForMember(
                                nameof(PaginationModel<IEntity>.Query),
                                opts => opts.MapFrom(nameof(Pagination<IEntity>.QueryOrFilter)));

                    cfg.CreateMap(typeof(PaginationModel<>), typeof(Pagination<>))
                            .ForMember(nameof(Pagination<IEntity>.QueryFunc), opts => opts.Ignore())
                            .ForMember(nameof(Pagination<IEntity>.QueryOrFilter), opts => opts.MapFrom(nameof(PaginationModel<IEntity>.Query)));

                    cfg.CreateMap<AuditRecord, AuditRecordModel>();
                    cfg.CreateMap<AuditRecordModel, AuditRecord>();
                    cfg.CreateMap<AuditPagination, AuditPaginationModel>();

                    cfg.CreateMap(typeof(ServiceResponse<>), typeof(ServiceResponse<>));
                });
        }
        private static void AddEntityConfigRecordsMappings(string mapperName, IEnumerable<EntityConfigRecord> entityConfigRecords)
        {
            MappingExtensions.AddConfiguration(mapperName, cfg =>
              {
                  foreach (var r in entityConfigRecords)
                  {
                      var mtt = r.EndpointSettings.MapToType;
                      if (mtt != r.Type)
                      {
                          cfg.CreateMap(r.Type, r.EndpointSettings.MapToType);
                          cfg.CreateMap(r.EndpointSettings.MapToType, r.Type);
                      }
                      var mtptType = r.EndpointSettings.MapToPaginationType;
                      var pType = typeof(Pagination<>).MakeGenericType(mtt);
                      if (mtptType != pType || mtptType != typeof(PaginationModel<>).MakeGenericType(mtt))
                      {
                          cfg.CreateMap(pType, mtptType);
                          cfg.CreateMap(mtptType, pType);
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

            // services.
            services.AddSingleton(config.EntityConfigRecords);
            //response mappers
            var mappers = config.EntityConfigRecords.Select(t => t.EndpointSettings.ResponseMapperType).ToArray();
            foreach (var m in mappers)
                services.TryAddSingleton(m);

            //mapper factory
            services.TryAddSingleton<IMapperFactory, DefaultMapperFactory>();
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
                var mt = wc.CurrentEntityConfigRecord?.EndpointSettings.ResponseMapperType ?? config.ServiceResponseMapperType;
                return sp.GetService(mt) as IServiceResponseMapper;
            });

            var auditManagerType = config.AuditSettings.Disabled ?
                typeof(DummyAuditManager) :
                typeof(AuditManager);

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

                ecr.Name ??= ecr.EndpointSettings != null && ecr.EndpointSettings.Area.HasValue() ?
                    $"{ecr.EndpointSettings?.Area}_{ecr.Type.Name}" :
                    ecr.Type.Name;

                var hasDuplication = temp.Where(e => e.Name == ecr.Name);
                if (hasDuplication.Count() > 1)
                    throw new InvalidOperationException($"Duplication in {nameof(EntityConfigRecord.Name)} field : {ecr.Name}. Please provide unique name for the controller. See configured entities where Routes equals {hasDuplication.First().EndpointSettings.Route} and {hasDuplication.Last().EndpointSettings.Route}");

                ecr.EventKeys ??= ekr;
                ecr.PermissionRecord ??= pr;
                ecr.EntityKey ??= fn;
                ecr.PaginationSettings ??= config.DefaultPaginationSettings;
                ecr.FilterFactoryType ??= config.FilterFactoryType;
                ecr.ModelPrepararType ??= config.ModelPrepararType;

                ecr.AuditSettings = NormalizeAudity(ecr, config.AuditSettings);
                ecr.EndpointSettings = NormalizeEndpointSettings(ecr, config);

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

        private static void AddAnyServiceControllers(AnyServiceConfig config)
        {
            var list = new List<EntityConfigRecord>(config.EntityConfigRecords);
            if (!config.AuditSettings.Disabled)
            {
                list.Add(new EntityConfigRecord
                {
                    Type = typeof(AuditRecord),
                    EndpointSettings = new EndpointSettings
                    {
                        Area = "__anyservice",
                        ControllerType = typeof(AuditController),
                    }
                });
            }
            if (config.UseLogRecordEndpoint)
            {
                list.Add(new EntityConfigRecord
                {
                    Type = typeof(LogRecord),
                    EndpointSettings = new EndpointSettings
                    {
                        Area = "__anyservice",
                        ControllerType = typeof(LogRecordController),
                    }
                });
            }
            config.EntityConfigRecords = list;
        }

        private static EndpointSettings NormalizeEndpointSettings(EntityConfigRecord ecr, AnyServiceConfig config)
        {
            var settings = (ecr.EndpointSettings ??= new EndpointSettings());
            if (settings.Disabled)
                return null;
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

            return settings;
        }

        private static void BuildControllerMethodSettings(EndpointSettings settings, EntityConfigRecord ecr)
        {
            var defaultControllerMethodSettings = new EndpointMethodSettings();
            settings.PostSettings ??= defaultControllerMethodSettings;
            settings.GetSettings ??= defaultControllerMethodSettings;
            settings.PutSettings ??= defaultControllerMethodSettings;
            settings.DeleteSettings ??= defaultControllerMethodSettings;

            if (
                settings.PostSettings.Disabled &&
                settings.GetSettings.Disabled &&
                settings.PutSettings.Disabled &&
                settings.DeleteSettings.Disabled)
                throw new ArgumentException($"Invalid operation: {nameof(EntityConfigRecord)} named {ecr.Name} has all httpMethods deactivated");
        }

        private static Type BuildController(Type entityType, EndpointSettings settings)
        {
            var mapToType = settings.MapToType;
            var isParent = mapToType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParentApiModel<>));

            return isParent
                ?
                typeof(GenericParentController<>).MakeGenericType(mapToType) :
                typeof(GenericController<,>).MakeGenericType(mapToType, entityType);
        }
        private static AuditSettings NormalizeAudity(EntityConfigRecord ecr, AuditSettings auditSettings)
        {
            if (auditSettings.Disabled)
                return new AuditSettings
                {
                    Disabled = true,
                    AuditRules = new AuditRules()
                };

            return ecr.AuditRules == null ?
                auditSettings :
                new AuditSettings
                {
                    Disabled = auditSettings.Disabled,
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
