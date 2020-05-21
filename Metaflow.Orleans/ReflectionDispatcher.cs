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

            return Task.FromResult(lambda(owner, resource));
        }
    }
}
