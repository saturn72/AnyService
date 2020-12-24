using AnyService.Infrastructure;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static ConcurrentDictionary<string, Action<IMapperConfigurationExpression>> _mapperConfigurations;
        private static IMapperFactory _mapperFactory;

        static MappingExtensions()
        {
            _mapperConfigurations = new ConcurrentDictionary<string, Action<IMapperConfigurationExpression>>();

        }
        public static void Build(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            _mapperFactory = scope.ServiceProvider.GetService(typeof(IMapperFactory)) as IMapperFactory;
            foreach (var mc in _mapperConfigurations)
            {
                var mapperConfig = new MapperConfiguration(mc.Value);
                _mapperFactory.AddMapper(mc.Key, mapperConfig.CreateMapper());
            }
        }
        public static void AddConfiguration(string mapperName, Action<IMapperConfigurationExpression> configuration, bool deleteExists = false)
        {
            var exist = _mapperConfigurations.ContainsKey(mapperName);
            var cfg = deleteExists || !exist ?
                configuration :
                (_mapperConfigurations[mapperName] += configuration);
            _mapperConfigurations[mapperName] = cfg;
        }
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
