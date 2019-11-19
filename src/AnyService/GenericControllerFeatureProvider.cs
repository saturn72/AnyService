using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyService
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IEnumerable<Type> _entities;

        public GenericControllerFeatureProvider(IEnumerable<Type> entities)
        {
            _entities = entities;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // This is designed to run after the default ControllerTypeProvider, 
            // so the list of 'real' controllers has already been populated.
            foreach (var entityType in _entities)
            {
                var typeName = entityType.Name + "Controller";
                if (!feature.Controllers.Any(t => t.Name == typeName))
                {
                    // There's no controller for this entity, so add the generic version.
                    var controllerType = typeof(GenericController<>)
                        .MakeGenericType(entityType).GetTypeInfo();
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}
