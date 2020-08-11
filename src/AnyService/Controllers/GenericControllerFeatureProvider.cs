using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyService.Controllers
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
            var ecrm = _serviceProvider.GetService<EntityConfigRecordManager>();
            
            var controllersToAdd = ecrm.EntityConfigRecords
                .Select(e => e.ControllerType)
                .Where(ct => existsControllers.All(c => c != ct));

            foreach (var cta in controllersToAdd)
                feature.Controllers.Add(cta.GetTypeInfo());
        }
    }
}