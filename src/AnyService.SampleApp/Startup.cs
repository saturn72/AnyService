using AnyService.Services;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Validators;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Services.FileStorage;
using Microsoft.Extensions.Hosting;

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
            var builder = services.AddMvc(o => o.EnableEndpointRouting = false);

            var entities = new[]
            {
                typeof(DependentModel),
                typeof(MultipartSampleModel)
            };
            var validators = new ICrudValidator[]
            {
                new DependentModelValidator(),
                new MultipartSampleValidator(),
            };
            
            services.AddAnyService(builder, Configuration, entities, validators);
            ConfigureLiteDb(services);
        }
        private void ConfigureLiteDb(IServiceCollection services)
        {
            var liteDbName = "anyservice-testsapp.db";
            services.AddTransient<IFileStoreManager>(sp => new LiteDb.FileStoreManager(liteDbName));
            //configure db repositories
            services.AddTransient<IRepository<DependentModel>>(sp => new LiteDb.Repository<DependentModel>(liteDbName));
            services.AddTransient<IRepository<MultipartSampleModel>>(sp => new LiteDb.Repository<MultipartSampleModel>(liteDbName));

            using var db = new LiteDatabase(liteDbName);
            var mapper = BsonMapper.Global;

            mapper.Entity<DependentModel>().Id(d => d.Id);
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
            app.UseMiddleware<AnyServiceMiddleware>();
            app.UseMvc();
        }
    }
}
