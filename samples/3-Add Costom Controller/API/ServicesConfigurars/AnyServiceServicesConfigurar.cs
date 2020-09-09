using AnyService;
using API.Controllers;
using API.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.ServiceConfigurars
{
    public class AnyServiceServicesConfigurar : IServicesConfigurar
    {
        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            var config = new AnyServiceConfig
            {
                EntityConfigRecords = new[]
                {
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        ControllerType = typeof(AdminProductController),
                    }
                }
            };
            services.AddAnyService(config);

            return services;
        }
    }
}
