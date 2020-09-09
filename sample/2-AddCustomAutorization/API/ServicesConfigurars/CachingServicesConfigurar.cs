using AnyService;
using AnyService.Caching;
using AnyService.EasyCaching;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.ServiceConfigurars
{
    public class CachingServicesConfigurar:IServicesConfigurar
    {
        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            var easycachingconfig = new EasyCachingConfig();
            configuration.GetSection("caching").Bind(easycachingconfig);

            services.AddSingleton(easycachingconfig);
            services.AddEasyCaching(options => options.UseInMemory("default"));
            services.AddSingleton<ICacheManager, EasyCachingCacheManager>();
            
            return services;
        }
    }
}
