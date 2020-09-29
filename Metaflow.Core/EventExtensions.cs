using System.Collections.Generic;

namespace Metaflow
{
    public static class EventExtensions
    {
        public static string Name<T, TInput>(this object @event)
        {
            if (@event is Created<T>) return "Created";
            if (@event is Rejected<T, TInput>) return "Rejected";
            if (@event is Deleted<T>) return "Deleted";
            if (@event is Ignored<T, TInput>) return "Ignored";
            if (@event is Replaced<T>) return "Replaced";
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
