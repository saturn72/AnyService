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
            var allEcrs = _serviceProvider.GetService<IEnumerable<EndpointSettings>>();

            var p = controller.Application.Properties;
            var genericTypeDefinition = controller.ControllerType.GetGenericTypeDefinition();
            if (typeof(GenericController<,>) != genericTypeDefinition)
                return;

            var currentEcrs = allEcrs.Where(es => es.ControllerType == controller.ControllerType);
            controller.ControllerName = allEcrs.First().ControllerName;

            var routeAtts = currentEcrs.Select(es => new RouteAttribute(es.Route)).ToArray();

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
