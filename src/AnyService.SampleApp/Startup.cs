using AnyService.Services;
using AnyService.SampleApp.Models;
using AnyService.SampleApp.Validators;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnyService.Services.FileStorage;

namespace AnyService.SampleApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }
        private IHostingEnvironment _env;
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddControllersAsServices();
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
            services.AddAnyService(Configuration, entities, validators);

            var liteDbName = "anyservice-testsapp.db";

            services.AddTransient<IFileStoreManager>(sp => new AnyService.LiteDb.FileStoreManager(liteDbName));

            //configure db repositories
            services.AddTransient<IRepository<DependentModel>>(sp => new AnyService.LiteDb.Repository<DependentModel>(liteDbName));
            services.AddTransient<IRepository<MultipartSampleModel>>(sp => new AnyService.LiteDb.Repository<MultipartSampleModel>(liteDbName));
            using (var db = new LiteDatabase(liteDbName))
            {
                var mapper = BsonMapper.Global;

                mapper.Entity<DependentModel>().Id(d => d.Id);
                mapper.Entity<MultipartSampleModel>().Id(d => d.Id);
            }
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            app.UseMiddleware<AnyServiceMiddleware>();
            app.UseMvc();
        }
    }
}
