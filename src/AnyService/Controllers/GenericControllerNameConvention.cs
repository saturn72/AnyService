using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
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
            controller.ControllerName = RouteMapper.TypeConfigRecords.FirstOrDefault(t => t.Type == entityType)?.RoutePrefix ?? entityType.Name;
        }
    }
}
