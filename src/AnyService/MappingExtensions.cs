using AutoMapper;
using System;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static IMapper _mapper;
        private static MapperConfiguration mc;

        public static void Configure(Action<IMapperConfigurationExpression> configure)
        {
            mc = new MapperConfiguration(configure);
            _mapper = null;
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
