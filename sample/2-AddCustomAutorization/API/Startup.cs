using AnyService;
using AnyService.Endpoints;
using API.ServiceConfigurars;
using API.ServicesConfigurars;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            //configure authentication
            services.AddAuthentication();

            new AnyServiceServicesConfigurar().Configure(services, Configuration, null);
            new CachingServicesConfigurar().Configure(services, Configuration, null);
            new EfServicesConfigurar().Configure(services, Configuration, null);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();

            app.UseAnyService();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAnyService();
                endpoints.MapControllers();
            });
        }
    }
}
