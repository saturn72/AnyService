using AnyService.Mapping;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using System;
using System.Collections.Concurrent;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static ConcurrentDictionary<string, Action<IMapperConfigurationExpression>> _mapperConfigurations;
        private static IMapperFactory _mapperFactory;
        private static IServiceProvider _serviceProvider;

        static MappingExtensions()
        {
            _mapperConfigurations = new ConcurrentDictionary<string, Action<IMapperConfigurationExpression>>();

        }
        public static void Build(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _mapperFactory = _serviceProvider.GetService(typeof(IMapperFactory)) as IMapperFactory;

            Action<IMapperConfigurationExpression> basicMapperConfig = cfg => cfg.AddExpressionMapping();

            foreach (var mc in _mapperConfigurations)
            {
                var cfg = basicMapperConfig += mc.Value;
                var mapperConfig = new MapperConfiguration(cfg);
                _mapperFactory.AddMapper(mc.Key, mapperConfig.CreateMapper());
            }
        }
        public static void AddConfiguration(string mapperName, Action<IMapperConfigurationExpression> configuration)
        {
            if (_mapperConfigurations.ContainsKey(mapperName))
                configuration = _mapperConfigurations[mapperName] += configuration;
            Configure(mapperName, configuration);
        }
        public static void Configure(string mapperName, Action<IMapperConfigurationExpression> configuration)
            => _mapperConfigurations[mapperName] = configuration;

        public static object Map(this object source, Type destination, string mapperName)
        {
            return _mapperFactory.GetMapper(mapperName).Map(source, source.GetType(), destination);
        }
        public static TDestination Map<TDestination>(this object source, string mapperName)
        {
            return (TDestination)Map(source, typeof(TDestination), mapperName);
        }
        public static TDestination Map<TSource, TDestination>(this TSource source, string mapperName)
        {
            return Map<TDestination>(source, mapperName);
        }
    }
}
