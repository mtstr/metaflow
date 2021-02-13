using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Metaflow.Orleans
{
    class EventSerializer : IEventSerializer
    {
        private static Type ResolvePropertyType(PropertyInfo pi)
        {
            var t = pi.PropertyType;

            var attr = pi.GetCustomAttribute<RestfulResourceAttribute>();
            if (attr != null)
            {
                return attr.ResourceType;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(FSharpOption<>))
            {
                return t.GetGenericArguments().First();
            }

            if (t.IsGenericType && t.IsAssignableTo(typeof(IEnumerable)))
            {
                return t.GetGenericArguments().First();
            }

            return t;
        }

        private static bool IsEvent<TOwner>(object @event, Type eventType, out string name)
        {
            List<Type> ownedTypes = typeof(TOwner).GetProperties().Select(ResolvePropertyType).ToList();
            var a = @event.GetType().GetGenericArguments().First();
            if (@event.GetType().GetGenericTypeDefinition() == eventType)
            {

                if (a == typeof(TOwner) || ownedTypes.Contains(a))
                {
                    name = a.Name;
                    return true;
                }

                if (a.IsGenericType && a.GetGenericTypeDefinition() == typeof(FSharpList<>))
                {
                    var b = a.GetGenericArguments().First();
                    if (ownedTypes.Contains(b))
                    {
                        name = $"{b.Name}[]";
                        return true;
                    }
                }
            }
            name = string.Empty;
            return false;
        }
        public string Name<TOwner, TResource, TInput>(object @event)
        {
            if (@event is Upgraded<TResource>) return $"Upgraded:{typeof(TResource).Name}";
            if (@event is Rejected<TInput>) return $"Rejected:{typeof(TInput).Name}";
            if (@event is Ignored<TInput>) return $"Ignored:{typeof(TInput).Name}";

            if (IsEvent<TOwner>(@event, typeof(Replaced<>), out string n1))
                return $"Replaced:{n1}";

            if (IsEvent<TOwner>(@event, typeof(Deleted<>), out string n2))
                return $"Deleted:{n2}";

            if (IsEvent<TOwner>(@event, typeof(Created<>), out string n3))
                return $"Created:{n3}";


            throw new Exception($"Expecting event for {typeof(TOwner).Name}/{typeof(TResource).Name}/{typeof(TInput).Name}, but received {@event.GetType().FullName}");
        }

        private static Type ResolveDataType(Type type, string dataTypeId)
        {
            List<Type> ownedTypes = type.GetProperties().Select(ResolvePropertyType).ToList();

            if (dataTypeId.EndsWith("[]"))
            {
                var dataTypeName = dataTypeId.Replace("[]", "");
                var dataType = ownedTypes.FirstOrDefault(ot => ot.Name == dataTypeName);
                if (dataType != null) return typeof(FSharpList<>).MakeGenericType(dataType);
            }
            else
            {
                return ownedTypes.FirstOrDefault(ot => ot.Name == dataTypeId);
            }

            return null;
        }
        public object Deserialize(Type type, string eventType, string json)
        {
            string[] split = eventType.Split(":");
            (var action, var data) = (split[0], split[1]);

            var eventDataType = ResolveDataType(type, data);

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