using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Controllers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericControllerNameConvention : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.GetGenericTypeDefinition() !=
                typeof(GenericController<>))
            {
                return;
            }
            var entityType = controller.ControllerType.GenericTypeArguments[0];
            var ecrm = AppEngine.GetService<EntityConfigRecordManager>();
            var tcr = ecrm.EntityConfigRecords.FirstOrDefault(t => t.Type == entityType);

            controller.ControllerName = tcr?.Route ?? entityType.Name;
        }
    }
}
