using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metaflow.Orleans
{
    public static class StateExtensions
    {
        public static GrainState<T> Apply<T>(this GrainState<T> state, object @event)
        {
            (T newState, bool exists) = @event switch
            {
                Deleted<T> _ => (default(T), false),
                Created<T> c => (c.After, true),
                Upgraded<T> c => (c.After, true),
                _ => (CalculateState(state.Value, @event), state.Exists)
            };

            return new GrainState<T> { Value = newState, Exists = exists };
        }

        private static T CalculateState<T>(T state, object @event)
        {
            Func<MethodInfo, Type, bool> match = (m, t) =>
            {
                List<ParameterInfo> p = m.GetParameters().ToList();
                return m.IsPublic && m.Name == "Apply" && m.ReturnType == typeof(T) && p.Count == 1 && p[0].ParameterType == t;
            };
            var mi = typeof(T).GetMethods().FirstOrDefault(m => match(m, @event.GetType()));

            return mi != null ? (T)mi.Invoke(state, new[] { @event }) : state;
        }
    }
}
