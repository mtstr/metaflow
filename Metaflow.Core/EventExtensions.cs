using System;
using System.Collections.Generic;
using System.Linq;

namespace Metaflow
{
    public static class EventExtensions
    {

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
