using AnyService.Security;
using AnyService.Services;
using AnyService.Services.FileStorage;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.LiteDb.SampleApp
{
    public class Stock : IDomainObject
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
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
            services
                    .AddMvcCore(o => o.EnableEndpointRouting = false)
                    .AddAuthorization();

            var entities = new[]
            {
                typeof(Stock)
            };

            services.AddAnyService(entities);
            ConfigureLiteDb(services);
            ConfigureCaching(services);
        }
        private void ConfigureCaching(IServiceCollection services)
        {
            //Configure caching here...
        }

        private void ConfigureLiteDb(IServiceCollection services)
        {
            var liteDbName = "anyservice-testsapp.db";
            services.AddTransient<IFileStoreManager>(sp => new LiteDbFileStoreManager(liteDbName));
            //configure db repositories
            services.AddTransient<IRepository<UserPermissions>>(sp => new LiteDbRepository<UserPermissions>(liteDbName));
            services.AddTransient<IRepository<Stock>>(sp => new LiteDbRepository<Stock>(liteDbName));

            using var db = new LiteDatabase(liteDbName);
            var mapper = BsonMapper.Global;

            mapper.Entity<Stock>().Id(d => d.Id);
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAnyService();
            app.UseMvc();
        }
    }
}
