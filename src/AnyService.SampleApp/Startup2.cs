using AnyService.Middlewares;
using AnyService.SampleApp.Models;
using AnyService.Services;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Services.FileStorage;
using Microsoft.Extensions.Hosting;
using AnyService.EasyCaching;
using AnyService.Core.Caching;
using AnyService.Core.Security;
using AnyService.LiteDb;

namespace AnyService.SampleApp
{
    public class Startup2
    {
        public Startup2(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = services.AddMvc(o => o.EnableEndpointRouting = false);

            var entities = new[]
            {
                typeof(DependentModel),
                typeof(Dependent2),
            };

            services.AddAnyService(builder, entities);

            ConfigureLiteDb(services);
            ConfigureCaching(services);
        }

        private void ConfigureCaching(IServiceCollection services)
        {
            services.AddSingleton<ICacheManager, EasyCachingCacheManager>();
            services.AddEasyCaching(options => options.UseInMemory("default"));

            var easycachingconfig = new EasyCachingConfig();
            Configuration.GetSection("caching").Bind(easycachingconfig);

            services.AddSingleton(easycachingconfig);
            services.AddSingleton<ICacheManager, EasyCachingCacheManager>();
        }

        private void ConfigureLiteDb(IServiceCollection services)
        {
            var liteDbName = "anyservice-testsapp.db";
            services.AddSingleton<IUserPermissionsRepository>(p => new UserPermissionRepository(liteDbName));
            services.AddTransient<IFileStoreManager>(sp => new FileStoreManager(liteDbName));
            //configure db repositories
            services.AddTransient<IRepository<DependentModel>>(sp => new LiteDb.Repository<DependentModel>(liteDbName));
            services.AddTransient<IRepository<Dependent2>>(sp => new LiteDb.Repository<Dependent2>(liteDbName));
            services.AddTransient<IRepository<MultipartSampleModel>>(sp => new LiteDb.Repository<MultipartSampleModel>(liteDbName));

            using var db = new LiteDatabase(liteDbName);
            var mapper = BsonMapper.Global;

            mapper.Entity<DependentModel>().Id(d => d.Id);
            mapper.Entity<Dependent2>().Id(d => d.Id);
            mapper.Entity<MultipartSampleModel>().Id(d => d.Id);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            //you may use app.UseAnyService() to setup anyservice pipeline instead the two lines below
            app.UseMiddleware<AnyServiceWorkContextMiddleware>();
            app.UseMiddleware<AnyServicePermissionMiddleware>();

            app.UseMvc();
        }
    }
}
