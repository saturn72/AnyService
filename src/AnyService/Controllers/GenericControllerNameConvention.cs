using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Controllers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericControllerNameConvention : Attribute, IControllerModelConvention
    {
        private static Func<IEnumerable<EntityConfigRecord>> _entityConfigRecordsResolver;
        public static void Init(IServiceProvider serviceProvider)
        {
            _entityConfigRecordsResolver = () => serviceProvider.GetService<IEnumerable<EntityConfigRecord>>();
        }
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.GetGenericTypeDefinition() !=
                typeof(GenericController<>))
            {
                return;
            }
            var entityType = controller.ControllerType.GenericTypeArguments[0];
            var ecrm = _entityConfigRecordsResolver();
            var tcr = ecrm.FirstOrDefault(t => t.Type == entityType && t.ControllerSettings.ControllerType == controller.ControllerType);

            controller.ControllerName = tcr.ControllerSettings.Route.Value ?? entityType.Name;
        }
    }
}
