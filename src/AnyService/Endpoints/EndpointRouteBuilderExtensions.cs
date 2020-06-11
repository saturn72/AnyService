using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace AnyService.Endpoints
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapAnyService(this IEndpointRouteBuilder builder)
        {
            var configRecords = builder.ServiceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();

            foreach (var cr in configRecords)
                builder.MapControllerRoute(cr.Name, cr.Route);
            return builder;
        }
    }
}
