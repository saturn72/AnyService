using AnyService.SampleApp.Models;
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
using AnyService.Utilities;
using Microsoft.Extensions.Logging;
using AnyService.Events;
using AnyService.Endpoints;

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
            var builder = services
                .AddMvcCore(o => o.EnableEndpointRouting = false)
                .AddAuthorization();

            services.AddAuthentication(ManagedAuthenticationHandler.Schema)
                .AddScheme<AuthenticationSchemeOptions, ManagedAuthenticationHandler>(ManagedAuthenticationHandler.Schema, options => { });
            services.AddAuthorization();

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
            var services = app.ApplicationServices;
            var exHandler = services.GetService<IExceptionHandler>();
            app.UseExceptionHandler(app => app.Run(ctx => exHandler.Handle(ctx, LoggingEvents.UnexpectedException.Name)));
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAnyService();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAnyService();
            });
        }
    }
}
