using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Metaflow.Orleans
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DeleteControllerConvention : Attribute, IControllerModelConvention
    {
        public string FormatPattern { get; set; }

        public void Apply(ControllerModel controller)
        {
            if (NotApplicable(controller)) return;

            var stateType = controller.ControllerType.GenericTypeArguments[0];
            var resourceType = controller.ControllerType.GenericTypeArguments[1];

            controller.ControllerName = stateType.Name;
            controller.Selectors[0].AttributeRouteModel.Template = controller.ControllerName.ToLowerInvariant();

            controller.Actions.First().Selectors[0].AttributeRouteModel.Template = "{id}/" + resourceType.Name.ToLowerInvariant();
        }

        private static bool NotApplicable(ControllerModel controller)
        {
            return !controller.ControllerType.IsGenericType || controller.ControllerType.GetGenericTypeDefinition() != typeof(DeleteController<,>);
        }
    }
}
