using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.FSharp.Core;

namespace Metaflow.Orleans
{
    class EventSerializer : IEventSerializer
    {
        private static Type ResolvePropertyType(PropertyInfo pi)
        {
            var t = pi.PropertyType;

            if (t.IsGenericType && t.GetGenericTypeDefinition()==typeof(FSharpOption<>))
            {
                return t.GetGenericArguments().First();
            }

            if (t.IsGenericType && t.IsAssignableTo(typeof(IEnumerable)))
            {
                return t.GetGenericArguments().First();
            }

            var attr = pi.GetCustomAttribute<RestfulResourceAttribute>();
            if (attr != null)
            {
                return attr.ResourceType;
            }

            return t;
        }

        public object Deserialize(Type type, string eventType, string json)
        {
            string[] split = eventType.Split(":");
            (var action, var data) = (split[0], split[1]);

            List<Type> ownedTypes = type.GetProperties().Select(ResolvePropertyType).ToList();

            var eventDataType = ownedTypes.FirstOrDefault(ot => ot.Name == data);

            var targetType = (action, data) switch
            {
                ("Created", _) when data == type.Name => typeof(Created<>).MakeGenericType(type),
                ("Upgraded", _) when data == type.Name => typeof(Upgraded<>).MakeGenericType(type),
                ("Created", _) when eventDataType != null => typeof(Created<>).MakeGenericType(eventDataType),

                ("Replaced", _) when eventDataType != null => typeof(Replaced<>).MakeGenericType(eventDataType),
                ("Deleted", _) when eventDataType != null => typeof(Deleted<>).MakeGenericType(eventDataType),
                ("Deleted", _) when data == type.Name => typeof(Created<>).MakeGenericType(type),
                _ => typeof(FakeEvent)
            };

            var e = System.Text.Json.JsonSerializer.Deserialize(json, targetType,
                new JsonSerializerOptions().Configure());
            return e;
        }
    }
}