using AnyService.Audity;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.Audit;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace AnyService
{
    public static class MappingExtensions
    {
        private const string DefaultMapperName = "default";
        private static ConcurrentDictionary<string, Action<IMapperConfigurationExpression>> _mapperConfigurations;
        private static IMapperFactory _mapperFactory;
        private static IServiceProvider _serviceProvider;

        static MappingExtensions()
        {
            _mapperConfigurations = new ConcurrentDictionary<string, Action<IMapperConfigurationExpression>>();
            _mapperConfigurations[DefaultMapperName] = AnyServiceMappingConfiguration;

        }
        public static void Build(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _mapperFactory = _serviceProvider.GetRequiredService<IMapperFactory>();
            foreach (var mc in _mapperConfigurations)
            {
                var cfg = mc.Value;
                var mapperConfig = new MapperConfiguration(cfg);
                _mapperFactory.AddMapper(mc.Key, mapperConfig.CreateMapper());
            }
        }
        public static void AddConfiguration(Action<IMapperConfigurationExpression> configuration, string mapperName = DefaultMapperName)
        {
            if (_mapperConfigurations.ContainsKey(mapperName))
                configuration = _mapperConfigurations[mapperName] += configuration;
            Configure(configuration, mapperName);
        }
        public static void Configure(Action<IMapperConfigurationExpression> configuration, string mapperName = DefaultMapperName)
            => _mapperConfigurations[mapperName] = configuration;
        private static void AnyServiceMappingConfiguration(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap(typeof(Pagination<>), typeof(PaginationModel<>))
                    .ForMember(
                        nameof(PaginationModel<IDomainEntity>.Query),
                        opts => opts.MapFrom(nameof(Pagination<IDomainEntity>.QueryOrFilter)));

            cfg.CreateMap<AuditRecord, AuditRecordModel>();
            cfg.CreateMap<AuditRecordModel, AuditRecord>();
            cfg.CreateMap<AuditPagination, AuditPaginationModel>();
        }
        public static object Map(this object source, Type destination, string mapperName = DefaultMapperName)
        {
            return _mapperFactory.GetMapper(mapperName).Map(source, source.GetType(), destination);
        }
        public static TDestination Map<TDestination>(this object source, string mapperName = DefaultMapperName)
        {
            return (TDestination)Map(source, typeof(TDestination), mapperName);
        }
        public static TDestination Map<TSource, TDestination>(this TSource source, string mapperName = DefaultMapperName)
        {
            return Map<TDestination>(source, mapperName);
        }
    }
}
