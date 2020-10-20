﻿using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyService.Conventions
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IServiceProvider _serviceProvider;

        public GenericControllerFeatureProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var existsControllers = feature.Controllers.Select(c => c.AsType());
            var ecrm = _serviceProvider.GetService<IEnumerable<EndpointSettings>>();

            var controllersToAdd = ecrm
                .Select(es => es.ControllerType)
                .Where(ct => existsControllers.All(c => c != ct));

            foreach (var cta in controllersToAdd)
                feature.Controllers.Add(cta.GetTypeInfo());
        }
    }
}