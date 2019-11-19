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
using System.Linq;

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
                typeof(Dependent2Model),
                typeof(MultipartSampleModel)
            };
            var validators = new ICrudValidator[]
            {
                new DependentModelValidator(),
                new Dependent2ModelValidator(),
                new MultipartSampleValidator(),
            };
            
            //use this command when route== entity name
            //services.AddAnyService(builder, Configuration, entities, validators);

            var typeConfigRecords = entities.Select(e =>
            {
                var fn = e.FullName.ToLower();
                var ekr = new EventKeyRecord(fn + "_created", fn + "_read", fn + "_update", fn + "_delete");
                var routePrefix = "/" + e.Name;
                if (e.Equals(typeof(Dependent2Model)))
                    routePrefix =  routePrefix.Replace("model", "", System.StringComparison.InvariantCultureIgnoreCase);
                return new TypeConfigRecord(e, routePrefix, ekr);
            });

            services.AddAnyService(builder, Configuration, typeConfigRecords, validators);
            ConfigureLiteDb(services);
        }
        private void ConfigureLiteDb(IServiceCollection services)
        {
            var liteDbName = "anyservice-testsapp.db";
            services.AddTransient<IFileStoreManager>(sp => new LiteDb.FileStoreManager(liteDbName));
            //configure db repositories
            services.AddTransient<IRepository<DependentModel>>(sp => new LiteDb.Repository<DependentModel>(liteDbName));
            services.AddTransient<IRepository<Dependent2Model>>(sp => new LiteDb.Repository<Dependent2Model>(liteDbName));
            services.AddTransient<IRepository<MultipartSampleModel>>(sp => new LiteDb.Repository<MultipartSampleModel>(liteDbName));

            using var db = new LiteDatabase(liteDbName);
            var mapper = BsonMapper.Global;

            mapper.Entity<DependentModel>().Id(d => d.Id);
            mapper.Entity<Dependent2Model>().Id(d => d.Id);
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
