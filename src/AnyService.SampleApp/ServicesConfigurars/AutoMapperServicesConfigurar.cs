using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.SampleApp.ServicesConfigurars
{
    public class AutoMapperServicesConfigurar : IServicesConfigurar
    {
        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {

            MappingExtensions.Configure(cfg =>
            {
                //cfg.CreateMap<Stock, StockModel>();
                //cfg.CreateMap<StockModel, Stock>();
            });
            return services;
        }
    }
}
