using AnyService.Audity;
using AnyService.Models;
using AnyService.Services;
using AnyService.Services.Audit;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService
{
    public static class MappingExtensions
    {
        private static IMapper _mapper;
        internal static bool WasConfigured;
        private static MapperConfiguration MapperConfiguration;
        private static ICollection<Action<IMapperConfigurationExpression>> CreateMapActions =
            new List<Action<IMapperConfigurationExpression>>
            {
                AnyServiceMappingConfiguration,
            };

        public static void AddConfiguration(Action<IMapperConfigurationExpression> configuration) => CreateMapActions.Add(configuration);

        public static void Configure(Action<IMapperConfigurationExpression> configuration)
        {
            CreateMapActions.Add(configuration);
            Configure();
        }

        public static void Configure()
        {
            var configuration = (Action<IMapperConfigurationExpression>)Delegate.Combine(CreateMapActions.ToArray());
            MapperConfiguration = new MapperConfiguration(configuration);
            _mapper = null;
            WasConfigured = true;
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
