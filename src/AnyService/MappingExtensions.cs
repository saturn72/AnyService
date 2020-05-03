using AnyService.Core;
using AnyService.Services;
using AutoMapper;
using System;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static IMapper _mapper;

        internal static bool WasConfigured;

        private static MapperConfiguration mc;

        public static void Configure(Action<IMapperConfigurationExpression> configure)
        {
            configure += AnyServiceMappingConfiguration;
            mc = new MapperConfiguration(configure);
            _mapper = null;
            WasConfigured = true;
        }

        private static void AnyServiceMappingConfiguration(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap(typeof(Pagination<>), typeof(PaginationApiModel<>))
                    .ForMember(nameof(PaginationApiModel<object>.Query), opts => opts.MapFrom(nameof(Pagination<IDomainModelBase>.QueryAsString)));
        }

        public static IMapper MapperInstance => _mapper ?? (_mapper = mc.CreateMapper());

        public static TDestination Map<TDestination>(this object source)
            where TDestination : class
        {
            return MapperInstance.Map<TDestination>(source);
        }
        public static TDestination Map<TSource, TDestination>(this TSource source)
            where TSource : class
            where TDestination : class
        {
            return Map<TDestination>(source); ;
        }
    }
}
