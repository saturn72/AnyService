using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService
{
    public interface IServicesConfigurar
    {
        IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env);
    }
}
