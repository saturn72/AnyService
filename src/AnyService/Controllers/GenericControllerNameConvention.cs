using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AnyService.Controllers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class GenericControllerNameConvention : Attribute, IControllerModelConvention
    {
        private static Func<EntityConfigRecordManager> _entityConfigManagerResolver;
        public static void Init(IServiceProvider serviceProvider)
        {
            _entityConfigManagerResolver = () => serviceProvider.GetService<EntityConfigRecordManager>();
        }
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.GetGenericTypeDefinition() !=
                typeof(GenericController<>))
            {
                return;
            }
            var entityType = controller.ControllerType.GenericTypeArguments[0];
            var ecrm = _entityConfigManagerResolver();
            var tcr = ecrm.EntityConfigRecords.FirstOrDefault(t => t.Type == entityType);

            controller.ControllerName = tcr?.Route ?? entityType.Name;
        }
    }
}
