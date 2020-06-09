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
            cfg.CreateMap(typeof(Pagination<>), typeof(PaginationModel<>))
                    .ForMember(nameof(PaginationModel<object>.Query), opts => opts.MapFrom(nameof(Pagination<IDomainModelBase>.QueryAsString)));
        }

        public static IMapper MapperInstance => _mapper ??= mc.CreateMapper();
        public static object Map(this object source, Type destination)
        {
            return MapperInstance.Map(source, source.GetType(), destination);
        }

        public static TDestination Map<TDestination>(this object source)
        {
            return (TDestination)Map(source, typeof(TDestination));
        }
        public static TDestination Map<TSource, TDestination>(this TSource source)
        {
            return Map<TDestination>(source); ;
        }
    }
}
