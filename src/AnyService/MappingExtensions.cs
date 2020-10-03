using AnyService.Audity;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.Audit;
using AutoMapper;
using System;
using System.Collections.Generic;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static IMapper _mapper;
        internal static bool WasConfigured;
        private static MapperConfiguration MapperConfiguration;
        public static void Configure(IEnumerable<EntityConfigRecord> records, Action<IMapperConfigurationExpression> configure)
        {
            Action<IMapperConfigurationExpression> configurar = c =>
            {
                AnyServiceMappingConfiguration(c);
                if (!records.IsNullOrEmpty())
                    EntityConfigRecordsConfiguration(records)(c);
                configure(c);
            };
            MapperConfiguration = new MapperConfiguration(configurar);
            _mapper = null;
            WasConfigured = true;
        }

        private static Action<IMapperConfigurationExpression> EntityConfigRecordsConfiguration(IEnumerable<EntityConfigRecord> records)
        {
            return cfg =>
            {
                foreach (var r in records)
                {
                    var mtt = r.EndpointSettings.MapToType;
                    if (mtt != r.Type)
                        cfg.CreateMap(r.Type, r.EndpointSettings.MapToType);

                    var pType = r.EndpointSettings.MapToPaginationType;
                    if (pType != typeof(Pagination<>).MakeGenericType(mtt) || pType != typeof(PaginationModel<>).MakeGenericType(mtt))
                        cfg.CreateMap(typeof(Pagination<>).MakeGenericType(mtt), r.EndpointSettings.MapToPaginationType);
                }
            };
        }

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
