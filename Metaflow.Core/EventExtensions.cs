using System;
using System.Collections.Generic;

namespace Metaflow
{
    public static class EventExtensions
    {
        public static string Name<TOwner, TResource, TInput>(this object @event)
        {
            if (@event is Created<TResource>) return $"Created:{typeof(TResource).Name}";
            if (@event is Created<TOwner>) return $"Created:{typeof(TOwner).Name}";
            if (@event is Upgraded<TResource>) return $"Upgraded:{typeof(TResource).Name}";
            if (@event is Rejected<TInput>) return $"Rejected:{typeof(TInput).Name}";
            if (@event is Deleted<TResource>) return $"Deleted:{typeof(TResource).Name}";
            if (@event is Ignored<TInput>) return $"Ignored:{typeof(TInput).Name}";
            if (@event is Replaced<TResource>) return $"Replaced:{typeof(TResource).Name}";
            throw new Exception($"Expecting event for {typeof(TResource).Name}/{typeof(TInput).Name}, but received {@event.GetType().FullName}");
        }


        public static IEnumerable<object> Created<T>(this T entity)
        {
            return new List<object>() { new Created<T>(entity) };
        }

        public static IEnumerable<object> Replaced<T>(this T after, T before)
        {
            return new List<object>() { new Replaced<T>(before, after) };
        }
    }
}
