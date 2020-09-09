using AnyService;
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
            var entities = new[] { typeof(Product) };
            services.AddAnyService(entities);

            return services;
        }
    }
}
