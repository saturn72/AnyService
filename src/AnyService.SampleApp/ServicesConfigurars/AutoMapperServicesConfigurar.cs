using AnyService.SampleApp.Domain;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.ServicesConfigurars
{
    public class AutoMapperServicesConfigurar
    {
        public void Configure(AnyServiceConfig anyServiceConfig)
        {
            MappingExtensions.Configure(anyServiceConfig.EntityConfigRecords,  cfg =>
            {
                cfg.CreateMap<CategoryModel, Category>()
                    .ForMember(dest => dest.AdminComment, mo => mo.Ignore());
            });
        }
    }
}
