using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyService.Controllers
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var entityTypes = EntityConfigRecordManager.EntityConfigRecords.Select(e => e.Type).ToArray();

            foreach (var et in entityTypes)
            {
                var typeName = et.Name + "Controller";
                if (!feature.Controllers.Any(t => t.Name == typeName))
                {
                    var controllerType = typeof(GenericController<>)
                        .MakeGenericType(et);
                    feature.Controllers.Add(controllerType.GetTypeInfo());
                }
            }
        }
    }
}