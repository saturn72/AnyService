﻿using Microsoft.AspNetCore.Builder;
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
            var configRecords = builder.ServiceProvider.GetRequiredService<IEnumerable<EntityConfigRecord>>();

            foreach (var cr in configRecords)
            {
                var cs = cr.EndpointSettings;
                if (cs.Area.HasValue())
                {
                    builder.MapAreaControllerRoute(cr.Identifier, cs.Area, cs.Route);
                }
                else
                {
                    builder.MapControllerRoute(cr.Identifier, cs.Route);
                }
            }
            return builder;
        }
    }
}
