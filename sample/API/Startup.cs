using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService;
using AnyService.Endpoints;
using API.ServiceConfigurars;
using API.ServicesConfigurars;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
