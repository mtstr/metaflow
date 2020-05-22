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
    public class DeleteSelfControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Type _genericControllerType;
        private readonly IEnumerable<Type> _entityTypes;

        public DeleteSelfControllerFeatureProvider(Type genericControllerType, IEnumerable<Type> grainTypes)
        {
            _genericControllerType = genericControllerType;
            _entityTypes = grainTypes;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var entityType in _entityTypes)
            {
                var name = entityType.Name;

                var method = entityType.GetMethods().FirstOrDefault(mi => mi.IsPublic && mi.Name.ToUpperInvariant() == "DELETE" && !mi.IsGenericMethod && !mi.GetParameters().Any());

                if (method == null) return;

                TypeInfo controllerType = BuildClosedGenericControllerType(_genericControllerType, entityType).GetTypeInfo();

                // if same or derived controller already defined
                if (feature.Controllers.Any(c => controllerType.IsAssignableFrom(c)))
                {
                    continue;
                }

                feature.Controllers.Add(controllerType);

            }
        }

        private static Type BuildClosedGenericControllerType(Type openControllerType, Type grainType)
        {
            List<Type> genericTypeParams = new List<Type> { grainType };

            Type genericControllerType = openControllerType.MakeGenericType(genericTypeParams.ToArray());

            return genericControllerType;
        }
    }
}
