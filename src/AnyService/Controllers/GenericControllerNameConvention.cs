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
        private static IEnumerable<Type> GenericControllerTypes = new[] { typeof(GenericController<,>), typeof(GenericParentController<>) };
        private static Func<IEnumerable<EntityConfigRecord>> _entityConfigRecordsResolver;
        public static void Init(IServiceProvider serviceProvider)
        {
            _entityConfigRecordsResolver = () => serviceProvider.GetService<IEnumerable<EntityConfigRecord>>();
        }
        public void Apply(ControllerModel controller)
        {
            var genericTypeDefinition = controller.ControllerType.GetGenericTypeDefinition();
            if (!GenericControllerTypes.Contains(genericTypeDefinition))
                return;

            var entityType = controller.ControllerType.GenericTypeArguments[0];
            var ecrm = _entityConfigRecordsResolver();

            //can be removed?
            //var ecr = ecrm.FirstOrDefault(t => t.Type == entityType && t.ControllerSettings.ControllerType == controller.ControllerType);
            var ecr = ecrm.FirstOrDefault(t => t.ControllerSettings.ControllerType == controller.ControllerType);

            controller.ControllerName = ecr.ControllerSettings.Route.Value ?? entityType.Name;
        }
    }
}
