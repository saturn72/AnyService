using AnyService;
using AnyService.EntityFramework;
using AnyService.Services;
using AnyService.Services.FileStorage;
using API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace API.ServicesConfigurars
{
    public class EfServicesConfigurar : IServicesConfigurar
    {
        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            var cs = configuration.GetConnectionString("DefaultConnection");
            if (!cs.HasValue())
                throw new InvalidOperationException("connection string not found");
            var options = new DbContextOptionsBuilder<JanusDbContext>().UseSqlServer(cs).Options;

            services.AddTransient<DbContext>(sp => new JanusDbContext(options));
            services.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddTransient<IFileStoreManager, EfFileStoreManager>();
         
            return services;
        }
    }
}
