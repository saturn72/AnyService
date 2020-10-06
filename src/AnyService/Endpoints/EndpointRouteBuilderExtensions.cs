using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AnyService.Endpoints
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapAnyService(this IEndpointRouteBuilder builder)
        {
            var entityConfigRecords = builder.ServiceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();

            foreach (var ecr in entityConfigRecords)
            {
                var es = ecr.EndpointSettings;
                if (es.Area.HasValue())
                {
                    builder.MapAreaControllerRoute(ecr.Name, es.Area, es.Route);
                }
                else
                {
                    builder.MapControllerRoute(ecr.Name, es.Route);
                }
            }
            return builder;
        }
    }
}
