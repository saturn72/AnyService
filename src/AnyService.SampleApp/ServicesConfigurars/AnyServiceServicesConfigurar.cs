using AnyService.Events;
using AnyService.Middlewares;
using AnyService.SampleApp.Controllers;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Services;
using AnyService.Services;
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
            var anyServiceConfig = new AnyServiceConfig
            {
                EntityConfigRecords = new[]
                {
                    new EntityConfigRecord
                    {
                        Type =   typeof(DependentModel),
                        Authorization = new AuthorizationInfo
                        {
                            ControllerAuthorizationNode = new AuthorizationNode{Roles = new[]{"some-role"}}
                        }
                    },
                    new EntityConfigRecord
                    {
                        Type = typeof(Dependent2),
                        Route = "/api/d/",
                        CrudValidatorType = typeof(Dependent2AlwaysTrueCrudValidator)
                    },

                    new EntityConfigRecord
                    {
                        Type = typeof(MultipartSampleModel),
                    },
                    new EntityConfigRecord
                    {
                        Route = "/v1/my-great-route",
                        Type = typeof(CustomModel),
                        ControllerType = typeof(CustomController),
                    },
                      new EntityConfigRecord
                    {
                        Type = typeof(CustomModel),
                        Name = "area2_cutomModel"
                    },
                }
            };

            services.AddAnyService(anyServiceConfig);

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
