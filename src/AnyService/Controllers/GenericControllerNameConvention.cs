using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private static ILogger<GenericControllerNameConvention> _logger;

        public static void Init(IServiceProvider serviceProvider)
        {
            _entityConfigRecordsResolver ??= () =>
            {
                using var scope = serviceProvider.CreateScope();
                return scope.ServiceProvider.GetService<IEnumerable<EntityConfigRecord>>();
            };
            _logger ??= serviceProvider.GetService<ILogger<GenericControllerNameConvention>>();
        }
        public void Apply(ControllerModel controller)
        {
            var genericTypeDefinition = controller.ControllerType.GetGenericTypeDefinition();
            if (!GenericControllerTypes.Contains(genericTypeDefinition))
            {
                _logger.LogDebug($"Failed to locate {nameof(ControllerModel)} in anyservice registery");
                return;
            }

            var controllerModelType = controller.ControllerType.GenericTypeArguments[0];
            var ecrs = _entityConfigRecordsResolver();
            var matchEcrs = ecrs.Where(e =>
                controllerModelType == e.EndpointSettings.MapToType &&
                e.EndpointSettings.ControllerType == controller.ControllerType);

            foreach (var ecr in matchEcrs)
            {
                var match = ecr.EndpointSettings.Route.Value ?? controllerModelType.Name;
                controller.ControllerName = match;
                _logger.LogDebug($"Found matched controller with route: {match}");

            }
        }
    }
}
