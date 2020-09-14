using AnyService.Audity;
using AnyService.Models;
using AnyService.Services;
using AutoMapper;
using System;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static IMapper _mapper;
        internal static bool WasConfigured;
        private static MapperConfiguration MapperConfiguration;
        public static void Configure(Action<IMapperConfigurationExpression> configure)
        {
            configure += AnyServiceMappingConfiguration;
            MapperConfiguration = new MapperConfiguration(configure);
            _mapper = null;
            WasConfigured = true;
        }
        private static void AnyServiceMappingConfiguration(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap(typeof(Pagination<>), typeof(PaginationModel<>))
                    .ForMember(
                        nameof(PaginationModel<IDomainModelBase>.Query), 
                        opts => opts.MapFrom(nameof(Pagination<IDomainModelBase>.QueryOrFilter)));

            cfg.CreateMap<AuditRecord, AuditRecordModel>();
            cfg.CreateMap<AuditRecordModel, AuditRecord>();
        }
        public static IMapper MapperInstance => _mapper ??= MapperConfiguration.CreateMapper();
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
