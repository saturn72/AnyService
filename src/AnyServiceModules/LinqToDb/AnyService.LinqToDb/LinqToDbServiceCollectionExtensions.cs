using LinqToDB.Configuration;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.LinqToDb
{
    public static class LinqToDbServiceCollectionExtensions
    {
        public static IServiceCollection AddLinqToDb(this IServiceCollection services, LinqToDbConnectionOptionsBuilder builder)
        {
            var co = builder.Build();
            services.AddTransient<DataConnection>(sp => new DataConnection(co));

            return services;
        }
    }
}
