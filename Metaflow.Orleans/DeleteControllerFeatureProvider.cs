using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Orleans;

namespace Metaflow.Orleans
{
    public class DeleteControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Type _genericControllerType;
        private readonly IEnumerable<Type> _entityTypes;

        public DeleteControllerFeatureProvider(Type genericControllerType, IEnumerable<Type> grainTypes)
        {
            _genericControllerType = genericControllerType;
            _entityTypes = grainTypes;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var entityType in _entityTypes)
            {

                var method = entityType.GetMethods().FirstOrDefault(mi => mi.IsPublic &&  mi.GetParameters().ToList().Count == 1);

                if (method == null) return;

                TypeInfo controllerType = BuildClosedGenericControllerType(_genericControllerType, entityType, method.GetParameters()[0].ParameterType).GetTypeInfo();

                // if same or derived controller already defined
                if (feature.Controllers.Any(c => controllerType.IsAssignableFrom(c)))
                {
                    continue;
                }

                feature.Controllers.Add(controllerType);

            }
        }


        private static (Type, Type ParameterType) AsPut(List<ParameterInfo> parameters)
        {
            return (typeof(HttpPutAttribute), parameters[0].ParameterType);
        }


        private static Type BuildClosedGenericControllerType(Type openControllerType, Type grainType, Type argType)
        {
            List<Type> genericTypeParams = new List<Type> { grainType, argType };

            Type genericControllerType = openControllerType.MakeGenericType(genericTypeParams.ToArray());

            return genericControllerType;
        }
    }
}
