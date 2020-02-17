using AnyService.SampleApp.Models;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Services.FileStorage;
using AnyService.EasyCaching;
using AnyService.Core.Caching;
using AnyService.Core.Security;
using AnyService.LiteDb;
using Microsoft.AspNetCore.Authentication;
using AnyService.SampleApp.Identity;
using AnyService.Services;

namespace AnyService.SampleApp
{
    public class Startup
    {
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

            var entities = new[]
            {
                typeof(DependentModel),
                typeof(Dependent2),
                typeof(MultipartSampleModel)
            };

            services.AddAuthentication(ManagedAuthenticationHandler.Schema)
                .AddScheme<AuthenticationSchemeOptions, ManagedAuthenticationHandler>(ManagedAuthenticationHandler.Schema, options => { });

            services.AddAnyService(entities);
            ConfigureLiteDb(services);
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

        private void ConfigureLiteDb(IServiceCollection services)
        {
            var liteDbName = "anyservice-testsapp.db";
            services.AddTransient<IFileStoreManager>(sp => new LiteDbFileStoreManager(liteDbName));
            //configure db repositories
            services.AddTransient<IRepository<UserPermissions>>(sp => new LiteDbRepository<UserPermissions>(liteDbName));
            services.AddTransient<IRepository<DependentModel>>(sp => new LiteDbRepository<DependentModel>(liteDbName));
            services.AddTransient<IRepository<Dependent2>>(sp => new LiteDbRepository<Dependent2>(liteDbName));
            services.AddTransient<IRepository<MultipartSampleModel>>(sp => new LiteDbRepository<MultipartSampleModel>(liteDbName));

            using var db = new LiteDatabase(liteDbName);
            var mapper = BsonMapper.Global;

            mapper.Entity<DependentModel>().Id(d => d.Id);
            mapper.Entity<Dependent2>().Id(d => d.Id);
            mapper.Entity<MultipartSampleModel>().Id(d => d.Id);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAnyService();
            app.UseMvc();
        }
    }
}
