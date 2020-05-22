using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Metaflow.Orleans
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DeleteSelfControllerConvention : Attribute, IControllerModelConvention
    {
        public string FormatPattern { get; set; }

        public void Apply(ControllerModel controller)
        {
            if (NotApplicable(controller)) return;

            var resourceType = controller.ControllerType.GenericTypeArguments[0];

            controller.ControllerName = resourceType.Name;
            controller.Selectors[0].AttributeRouteModel.Template = controller.ControllerName.ToLowerInvariant();
        }

        private static bool NotApplicable(ControllerModel controller)
        {
            return !controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(DeleteSelfController<>);
        }
    }
}
