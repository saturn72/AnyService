using AnyService;
using AnyService.EntityFramework;
using AnyService.Services;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EfMappingServiceCollectionExtensions
    {
        public static IServiceCollection AddEfMappingRepository(this IServiceCollection services,
Func<EfMappingConfiguration> config) => AddEfMappingRepository(services, config());

        public static IServiceCollection AddEfMappingRepository(this IServiceCollection services, EfMappingConfiguration config)
        {
            if (!config.MapperName.HasValue())
                throw new InvalidOperationException($"{nameof(EfMappingConfiguration.MapperName)} is required");

            config.EntitiesToDbModelsMaps ??= new Dictionary<Type, Type>();
            var paginationType = typeof(Pagination<>);
            Action<IMapperConfigurationExpression> c = cfg =>
            {
                cfg.AddExpressionMapping();
                foreach (var kvp in config.EntitiesToDbModelsMaps)
                {
                    cfg.CreateMap(kvp.Key, kvp.Value);
                    cfg.CreateMap(kvp.Value, kvp.Key);
                    cfg.CreateMap(
                        paginationType.MakeGenericType(kvp.Key),
                        paginationType.MakeGenericType(kvp.Value));
                    cfg.CreateMap(
                       paginationType.MakeGenericType(kvp.Value),
                       paginationType.MakeGenericType(kvp.Key));
                }
            };
            MappingExtensions.AddConfiguration(config.MapperName, c);

            services.TryAddSingleton(config);

            var repoType = typeof(IRepository<>);
            var efMapRepoType = typeof(EfMappingRepository<,>);
            foreach (var kvp in config.EntitiesToDbModelsMaps)
            {
                var srv = repoType.MakeGenericType(kvp.Value);
                var impl = efMapRepoType.MakeGenericType(kvp.Key, kvp.Value);
                services.TryAddTransient(srv, impl);
            }
            return services;
        }
    }
}
