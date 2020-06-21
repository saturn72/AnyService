using AnyService.Events;
using AnyService.Middlewares;
using AnyService.SampleApp.Models;
using AnyService.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnyService.SampleApp.ServicesConfigurars
{
    public class AnyServiceServicesConfigurar : IServicesConfigurar
    {
        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            var entities = new[]
              {
                typeof(DependentModel),
                typeof(Dependent2),
                typeof(MultipartSampleModel),
            };

            services.AddAnyService(entities);
            services.AddSingleton<IExceptionHandler>(sp =>
            {
                var idg = sp.GetService<IIdGenerator>();
                var l = sp.GetService<ILogger<DefaultExceptionHandler>>();
                var eb = sp.GetService<IEventBus>();
                return new DefaultExceptionHandler(idg, l, eb, sp);
            });
            return services;
        }
    }
}
