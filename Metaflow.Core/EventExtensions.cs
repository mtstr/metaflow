using System.Collections.Generic;

namespace Metaflow
{
    public static class EventExtensions
    {
        public static string Name<T, TInput>(this object @event)
        {
            if (@event is Created<T>) return $"Created:{typeof(T).Name}";
            if (@event is Upgraded<T>) return $"Upgraded:{typeof(T).Name}";
            if (@event is Rejected<TInput>) return $"Rejected:{typeof(TInput).Name}";
            if (@event is Deleted<T>) return $"Deleted:{typeof(T).Name}";
            if (@event is Ignored<TInput>) return $"Ignored:{typeof(TInput).Name}";
            if (@event is Replaced<T>) return $"Replaced:{typeof(T).Name}";
            return "Unknown";
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
