using AnyService.Infrastructure;
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
            var existsControllers = feature.Controllers.Select(c => c.AsType());
            var ecrm = AnyServiceAppContext.GetService<EntityConfigRecordManager>();
            
            var controllersToAdd = ecrm.EntityConfigRecords
                .Select(e => e.ControllerType)
                .Where(ct => existsControllers.All(c => c != ct));

            foreach (var cta in controllersToAdd)
                feature.Controllers.Add(cta.GetTypeInfo());
        }
    }
}