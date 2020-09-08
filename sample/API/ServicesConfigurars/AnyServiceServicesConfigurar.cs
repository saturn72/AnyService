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
            var config = new AnyServiceConfig
            {
                EntityConfigRecords = new[]
                {
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        Authorization = new AuthorizationInfo
                        {
                            PostAuthorizationNode = new AuthorizationNode{Roles = new[]{"product-create" } },
                            GetAuthorizationNode = new AuthorizationNode{Roles = new[]{"product-read" } },
                            PutAuthorizationNode = new AuthorizationNode{Roles = new[]{"product-update" } },
                            DeleteAuthorizationNode = new AuthorizationNode{Roles = new[]{"product-delete" } },
                        }
                    }
                }
            };
            services.AddAnyService(config);

            return services;
        }
    }
}
