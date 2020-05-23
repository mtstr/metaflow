using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metaflow
{
    public static class ReflectionExtensions
    {
        public static MethodInfo DeleteSelfMethod(this Type grainType)
        {
            static bool match(MethodInfo mi, Type grainType)
            => mi.Name.ToUpperInvariant() == "DELETE"
                && mi.IsPublic
                && ReturnTypeMatches(mi, grainType)
                && !mi.GetParameters().Any();

            return grainType.GetMethods().FirstOrDefault(mi => match(mi, grainType));
        }

        public static IEnumerable<MethodInfo> MatchingMethods(this Type grainType, MutationRequest request, bool matchSignature = true)
        {
            static bool match(MethodInfo mi, MutationRequest request, bool matchSignature)
            {
                var p = mi.GetParameters().ToList();
                return mi.IsPublic && mi.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant() && p.Count == 1 && (!matchSignature || ReturnTypeMatches(mi, p[0].ParameterType));
            }

            return grainType.GetMethods().Where(mi => match(mi, request, matchSignature));
        }
        public static MethodInfo For(this IEnumerable<MethodInfo> methods, Type resourceType, Type inputType)
        {
            static bool match(MethodInfo mi, Type resourceType, Type inputType)
            {
                var p = mi.GetParameters().ToList();
                return ReturnTypeMatches(mi, resourceType)
                        && ParameterMatches(mi, inputType);
            }

            return methods.FirstOrDefault(mi => match(mi, resourceType, inputType));
        }

        private static bool ParameterMatches(MethodInfo mi, Type resourceType)
        {
            var p = mi.GetParameters().ToList();

            return p.Count == 1 && p[0].ParameterType == resourceType;
        }

        private static bool ReturnTypeMatches(MethodInfo mi, Type resourceType)
        => mi.ReturnType.IsGenericType
            && mi.ReturnType.GetGenericTypeDefinition() == typeof(Result<>)
            && mi.ReturnType.GenericTypeArguments[0] == resourceType;

    }
}
