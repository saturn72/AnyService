using AnyService.SampleApp.Entities;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Configurars
{
    public class AutoMapperServicesConfigurar
    {
        public void Configure()
        {
            MappingExtensions.Configure(cfg =>
            {
                cfg.CreateMap<CategoryModel, Category>()
                    .ForMember(dest => dest.AdminComment, mo => mo.Ignore());
            });
        }
    }
}
