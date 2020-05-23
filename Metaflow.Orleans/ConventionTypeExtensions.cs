using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Metaflow.Orleans
{
    internal static class ConventionTypeExtensions
    {
        internal static (Type grainType, Type resourceType) ControllerTypes(this ControllerModel controller)
        {
            if (controller.ControllerType.BaseType.IsGenericType && controller.ControllerType.BaseType.GetGenericTypeDefinition() == typeof(GrainController<,>))
            {
                var ga = controller.ControllerType.BaseType.GenericTypeArguments;
                return (ga[0], ga[1]);
            }

            return (null, null);
        }
    }
}
