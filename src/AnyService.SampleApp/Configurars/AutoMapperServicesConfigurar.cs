using AnyService.SampleApp.Entities;
using AnyService.SampleApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.SampleApp.Configurars
{
    public class AutoMapperServicesConfigurar
    {
        public void Configure(IServiceCollection services, AnyServiceConfig config)
        {
            MappingExtensions.AddConfiguration(services, config.MapperName, cfg =>
            {
                cfg.CreateMap<CategoryModel, Category>()
                    .ForMember(dest => dest.AdminComment, mo => mo.Ignore());
            });
        }
    }
}
