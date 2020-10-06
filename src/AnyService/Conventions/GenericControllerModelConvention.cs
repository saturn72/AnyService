using AnyService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Conventions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericControllerModelConvention : Attribute, IControllerModelConvention
    {
        private static IServiceProvider _serviceProvider;

        public static void Init(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public void Apply(ControllerModel controller)
        {
            var allEcrs = _serviceProvider.GetService<IEnumerable<EntityConfigRecord>>();

            var genericControllerTypes = new[] { typeof(GenericController<,>), typeof(GenericParentController<>) };

            var p = controller.Application.Properties;
            var genericTypeDefinition = controller.ControllerType.GetGenericTypeDefinition();
            if (!genericControllerTypes.Contains(genericTypeDefinition))
                return;

            var currentEcrs = allEcrs.Where(e =>
                e.EndpointSettings.ControllerType == controller.ControllerType);
            controller.ControllerName = allEcrs.First().EndpointSettings.ControllerName;

            var routeAtts = currentEcrs.Select(e => new RouteAttribute(e.EndpointSettings.Route)).ToArray();

            foreach (var ra in routeAtts)
            {
                var routeToAdd = new AttributeRouteModel(ra);
                var sm = new SelectorModel
                {
                    AttributeRouteModel = routeToAdd
                };
                controller.Selectors.Add(sm);
            }
        }
    }
}
