using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metaflow
{
    public static class ReflectionExtensions
    {
        public static MethodInfo SelfMethod(this Type grainType, Operation request)
        {
            static bool match(MethodInfo mi, Type grainType, Operation request)
            => mi.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant()
                && mi.IsPublic
                && ReturnTypeMatches(mi)
                && !mi.GetParameters().Any();

            return grainType.GetMethods().FirstOrDefault(mi => match(mi, grainType, request));
        }


        public static IEnumerable<MethodInfo> MatchingMethods(this Type grainType, Operation request, bool matchSignature = true)
        {
            static bool match(MethodInfo mi, Operation request, bool matchSignature)
            {
                var n = mi.Name;
                List<ParameterInfo> p = mi.GetParameters().ToList();
                return mi.IsPublic && mi.Name.ToUpperInvariant().StartsWith(request.ToString().ToUpperInvariant()) && p.Count == 1 && (!matchSignature || ReturnTypeMatches(mi));
            }

            return grainType.GetMethods().Where(mi => match(mi, request, matchSignature));
        }
        public static MethodInfo For(this IEnumerable<MethodInfo> methods, Type resourceType, Type inputType)
        {
            static bool match(MethodInfo mi, Type resourceType, Type inputType)
            {
                List<ParameterInfo> p = mi.GetParameters().ToList();
                return ReturnTypeMatches(mi)
                        && ParameterMatches(mi, inputType);
            }

            return methods.FirstOrDefault(mi => match(mi, resourceType, inputType));
        }

        public static RestfulAttribute RestfulAttribute(this Type type)
        {
            return type.GetCustomAttributes().OfType<RestfulAttribute>()
                .FirstOrDefault();
        }

        public static int ModelVersion(this Type type)
        {
            return type.RestfulAttribute().Version;
        }

        private static bool ParameterMatches(MethodInfo mi, Type resourceType)
        {
            List<ParameterInfo> p = mi.GetParameters().ToList();

            return p.Count == 1 && p[0].ParameterType == resourceType;
        }

        private static bool ReturnTypeMatches(MethodInfo mi)
        => typeof(IEnumerable<object>).IsAssignableFrom(mi.ReturnType);

    }
}
