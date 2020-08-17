using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metaflow
{
    public static class ReflectionExtensions
    {
        public static MethodInfo SelfMethod(this Type grainType, MutationRequest request)
        {
            static bool match(MethodInfo mi, Type grainType, MutationRequest request)
            => mi.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant()
                && mi.IsPublic
                && ReturnTypeMatches(mi)
                && !mi.GetParameters().Any();

            return grainType.GetMethods().FirstOrDefault(mi => match(mi, grainType, request));
        }

        public static IEnumerable<(MethodInfo, Type)> DeleteById(this Type grainType)
        {
            static Type match(MethodInfo mi, Type grainType)
            {
                var p = mi.GetParameters().ToList();

                var attr = mi.GetCustomAttributes().OfType<RestfulResourceAttribute>().FirstOrDefault();

                var res = mi.Name.ToUpperInvariant().StartsWith(MutationRequest.DELETE.ToString().ToUpperInvariant())
                && mi.IsPublic
                && attr != null
                && ReturnTypeMatches(mi)
                && p.Count == 1 && p[0].ParameterType == typeof(string);

                if (res) return attr.ResourceType;
                else return null;
            };

            return grainType.GetMethods().Select(mi => (mi, match(mi, grainType))).Where(mit => mit.Item2 != null);
        }

        public static IEnumerable<MethodInfo> MatchingMethods(this Type grainType, MutationRequest request, bool matchSignature = true)
        {
            static bool match(MethodInfo mi, MutationRequest request, bool matchSignature)
            {
                var n = mi.Name;
                var p = mi.GetParameters().ToList();
                return mi.IsPublic && mi.Name.ToUpperInvariant().StartsWith(request.ToString().ToUpperInvariant()) && p.Count == 1 && (!matchSignature || ReturnTypeMatches(mi));
            }

            return grainType.GetMethods().Where(mi => match(mi, request, matchSignature));
        }
        public static MethodInfo For(this IEnumerable<MethodInfo> methods, Type resourceType, Type inputType)
        {
            static bool match(MethodInfo mi, Type resourceType, Type inputType)
            {
                var p = mi.GetParameters().ToList();
                return ReturnTypeMatches(mi)
                        && ParameterMatches(mi, inputType);
            }

            return methods.FirstOrDefault(mi => match(mi, resourceType, inputType));
        }

        private static bool ParameterMatches(MethodInfo mi, Type resourceType)
        {
            var p = mi.GetParameters().ToList();

            return p.Count == 1 && p[0].ParameterType == resourceType;
        }

        private static bool ReturnTypeMatches(MethodInfo mi)
        => typeof(IEnumerable<object>).IsAssignableFrom(mi.ReturnType);

    }
}
