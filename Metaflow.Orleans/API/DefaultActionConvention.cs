using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Metaflow.Orleans
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultActionConvention : Attribute, IControllerModelConvention
    {
        public const string DefaultRoute = "{id}";

        public void Apply(ControllerModel controller)
        {
            var (grainType, resourceType) = controller.ControllerTypes();

            if (grainType == null || resourceType == null) return;

            var routeAttr = controller.Actions.First().Selectors[0].AttributeRouteModel;

            if (routeAttr?.Template == DefaultRoute && resourceType != grainType)
                routeAttr.Template = "{id}/" + resourceType.Name.ToLowerInvariant();

            if (controller.ControllerType.IsGenericType && controller.ControllerType.GetGenericTypeDefinition() == typeof(DeleteByIdController<,>))
                routeAttr.Template = "{id}/" + resourceType.Name.ToLowerInvariant() + "/{itemId}";

        }

    }
}
