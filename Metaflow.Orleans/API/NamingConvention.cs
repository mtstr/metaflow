using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Metaflow.Orleans
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class NamingConvention : Attribute, IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            (var grainType, var resourceType) = controller.ControllerTypes();
            if (grainType == null || resourceType == null) return;

            controller.ControllerName = grainType.Name;

            controller.Selectors[0].AttributeRouteModel.Template = controller.ControllerName.ToLowerInvariant();
        }
    }
}
