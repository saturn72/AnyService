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

            var controllerModelType = controller.ControllerType.GenericTypeArguments[0];
            var ecrm = _entityConfigRecordsResolver();

            var matchEcrs = ecrm.Where(e =>
                controllerModelType == e.EndpointSettings.MapToType &&
                e.EndpointSettings.ControllerType == controller.ControllerType);

            foreach (var ecr in matchEcrs)
                controller.ControllerName = ecr.EndpointSettings.Route.Value ?? controllerModelType.Name;
        }
    }
}
