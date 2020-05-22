using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Metaflow
{
    public class ReflectionDispatcher<T> : IDispatcher<T>
    where T : class, new()
    {
        public virtual Task<Result<TResource>> Invoke<TResource>(T owner, MutationRequest request, TResource resource)
        {
            if (request == MutationRequest.DELETE)
            {
                if (resource != null) return DispatchDelete<TResource>(owner, resource);
                return DispatchSelfDelete<TResource>(owner);
            }
            else
            {
                return DispatchUpdate<TResource>(owner, request, resource);
            }
        }

        private Task<Result<TResource>> DispatchSelfDelete<TResource>(T owner)
        {
            Func<MethodInfo, bool> methodPredicate = (MethodInfo mi) =>
            {
                var p = mi.GetParameters().ToList();
                return mi.Name.ToUpperInvariant() == "DELETE" && mi.IsPublic && p.Count == 0;
            };

            MethodInfo mi = typeof(T).GetMethods().FirstOrDefault(methodPredicate);

            if (mi == null) throw new InvalidOperationException();

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, Result<TResource>> lambda = Expression.Lambda<Func<T, Result<TResource>>>(methodCall, stateParam).Compile();

            return Task.FromResult(lambda(owner));
        }

        private Task<Result<TResource>> DispatchDelete<TResource>(T owner, TResource resource)
        {
            Func<MethodInfo, bool> methodPredicate = (MethodInfo mi) =>
            {
                var p = mi.GetParameters().ToList();
                return mi.IsPublic && mi.Name.ToUpperInvariant() == "DELETE" && p.Count == 1 && mi.GetParameters()[0].ParameterType == typeof(TResource);
            };

            MethodInfo mi = typeof(T).GetMethods().FirstOrDefault(methodPredicate);

            if (mi == null) throw new InvalidOperationException();

            ParameterExpression resourceParam = Expression.Parameter(typeof(TResource));

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi, resourceParam);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, TResource, Result<TResource>> lambda = Expression.Lambda<Func<T, TResource, Result<TResource>>>(methodCall, stateParam, resourceParam).Compile();

            return Task.FromResult(lambda(owner, resource));
        }

        private static Task<Result<TResource>> DispatchUpdate<TResource>(T owner, MutationRequest request, object arg)
        {
            Func<MethodInfo, bool> methodPredicate = (MethodInfo mi) =>
            {
                var p = mi.GetParameters().ToList();
                return p.Count == 1 && p[0].ParameterType == typeof(TResource);
            };

            MethodInfo mi = typeof(T).GetMethods().FirstOrDefault(m => m.IsPublic && m.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant() && methodPredicate(m));

            if (mi == null) throw new InvalidOperationException();

            ParameterExpression resourceParam = Expression.Parameter(typeof(TResource));

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi, resourceParam);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, TResource, Result<TResource>> lambda = Expression.Lambda<Func<T, TResource, Result<TResource>>>(methodCall, stateParam, resourceParam).Compile();

            return Task.FromResult(lambda(owner, (TResource)arg));
        }
    }
}
