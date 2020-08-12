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
    public class GrainControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly IEnumerable<Type> _grainTypes;

        public GrainControllerFeatureProvider(IEnumerable<Type> grainTypes)
        {
            _grainTypes = grainTypes;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var entityType in _grainTypes)
            {
                foreach (var kv in typeResolvers)
                {
                    var types = kv.Value(entityType);

                    foreach (var t in types)
                    {
                        TypeInfo controllerType = BuildClosedGenericControllerType(kv.Key, entityType, t).GetTypeInfo();

                        // if same or derived controller already defined
                        if (feature.Controllers.Any(c => controllerType.IsAssignableFrom(c)))
                        {
                            continue;
                        }

                        feature.Controllers.Add(controllerType);
                    }
                }
            }
        }

        private static readonly Dictionary<Type, Func<Type, List<Type>>> typeResolvers = new Dictionary<Type, Func<Type, List<Type>>>
        {
            [typeof(GetController<>)] = t => new List<Type>() { t },
            [typeof(DeleteController<,>)] = t => FindTypes(t, MutationRequest.DELETE),
            [typeof(PutController<,>)] = t => FindTypes(t, MutationRequest.PUT),
            [typeof(DeleteByIdController<,>)] = t =>
                t.GenericDelete().Select(m => m.ReturnType.GenericTypeArguments[0]).ToList(),
            [typeof(PatchController<,>)] = t => FindTypes(t, MutationRequest.PATCH),
            [typeof(PostController<,>)] = t =>
            {
                var selfCreate = t.SelfMethod(MutationRequest.POST);
                var all = FindTypes(t, MutationRequest.POST);

                if (selfCreate != null)
                    all.Add(t);

                return all;
            },
            [typeof(DeleteSelfController<>)] = t =>
            {
                var selfDeleteAvailable = t.SelfMethod(MutationRequest.DELETE);
                if (selfDeleteAvailable != null) return new List<Type>() { t };
                return new List<Type>();
            }
        };

        private static List<Type> FindTypes(Type t, MutationRequest request)
        {
            var all = t.MatchingMethods(request).ToList();

            return all.Select(mi => ResolveType(t, mi, request)).Where(t => t != null).ToList();
        }

        private static Type ResolveType(Type grainType, MethodInfo mi, MutationRequest request)
        {
            var parameterType = mi.GetParameters().FirstOrDefault()?.ParameterType;

            if (request != MutationRequest.PATCH) return parameterType;

            return grainType.GetCustomAttributes().OfType<RestfulAttribute>()
                                            .FirstOrDefault()?.DeltaType;
        }

        private static Type BuildClosedGenericControllerType(Type openControllerType, Type grainType, Type resourceType)
        {
            List<Type> genericTypeParams = new List<Type> { grainType };

            if (resourceType != grainType || openControllerType.GetGenericTypeDefinition() == typeof(PostController<,>)) genericTypeParams.Add(resourceType);

            Type genericControllerType = openControllerType.MakeGenericType(genericTypeParams.ToArray());

            return genericControllerType;
        }
    }
}
