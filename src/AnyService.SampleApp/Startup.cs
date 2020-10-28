using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Services.FileStorage;
using AnyService.EasyCaching;
using AnyService.Caching;
using Microsoft.AspNetCore.Authentication;
using AnyService.SampleApp.Identity;
using AnyService.Services;
using Microsoft.EntityFrameworkCore;
using AnyService.EntityFramework;
using AnyService.Middlewares;
using AnyService.Endpoints;
using AnyService.SampleApp.Hubs;
using Microsoft.AspNetCore.Http;
using AnyService.SampleApp.Configurars;

namespace AnyService.SampleApp
{
    public class Startup
    {
        public const string DbName = "anyservice-testsapp-db";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
                //.AddMvcCore(o => o.EnableEndpointRouting = false)
                //.AddAuthorization();

            services.AddSignalR();
            services.AddAuthentication(ManagedAuthenticationHandler.Schema)
                .AddScheme<AuthenticationSchemeOptions, ManagedAuthenticationHandler>(ManagedAuthenticationHandler.Schema, options => { });
            services.AddAuthorization();

            var anyServiceConfig = new AnyServiceConfigurar().Configure(services);
            new AutoMapperServicesConfigurar().Configure(anyServiceConfig);

            ConfigureEntityFramework(services);
            ConfigureCaching(services);
        }

        private void ConfigureCaching(IServiceCollection services)
        {
            var easycachingconfig = new EasyCachingConfig();
            Configuration.GetSection("caching").Bind(easycachingconfig);

            services.AddSingleton(easycachingconfig);
            services.AddEasyCaching(options => options.UseInMemory("default"));
            services.AddSingleton<ICacheManager, EasyCachingCacheManager>();
        }
        private void ConfigureEntityFramework(IServiceCollection services)
        {
            //setup entity framework provider here.
            //this is inmemory provider
            var options = new DbContextOptionsBuilder<SampleAppDbContext>()
                .UseInMemoryDatabase(databaseName: DbName).Options;
            services.AddTransient<DbContext>(sp => new SampleAppDbContext(options));
            services.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddTransient<IFileStoreManager, EfFileStoreManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var config = app.ApplicationServices.GetRequiredService<AnyServiceConfig>();
            if (config.UseErrorEndpointForExceptionHandling)
                app.UseExceptionHandler("/__error");
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseAuthentication();

            //we need to customize what happens when user is not authenticated in order to enable notifications
            var handler = OnMissingUserIdWorkContextMiddlewareHandlers.PermittedPathsOnMissingUserIdHandler(new[] { new PathString("/chathub"), new PathString("/notify") });
            app.UseMiddleware<WorkContextMiddleware>(handler);
            app.UseAnyService(useWorkContextMiddleware: false);

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAnyService();
                endpoints.MapHub<ChatHub>("/chatHub");
            });
        }
    }
}