using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Endpoints
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapAnyService(this IEndpointRouteBuilder builder)
        {
            var entityConfigRecords = builder.ServiceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();

            foreach (var es in entityConfigRecords.SelectMany(e => e.EndpointSettings))
            {
                if (es.Area.HasValue())
                {
                    builder.MapAreaControllerRoute(es.Name, es.Area, es.Route);
                }
                else
                {
                    builder.MapControllerRoute(es.Name, es.Route);
                }
            }
            return builder;
        }
    }
}
