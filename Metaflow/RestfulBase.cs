using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Metaflow
{
    public abstract class RestfulBase<T> : IRestful<T>
    where T : class, new()
    {
        public abstract Task<T> Get();

        public async Task<Result<TResource>> Handle<TResource>(MutationRequest request, TResource resource) where TResource : class, new()
        {
            Func<MethodInfo, bool> methodPredicate = (MethodInfo mi) =>
              {
                  var p = mi.GetParameters().ToList();
                  return p.Count == 1 && p[0].ParameterType == typeof(TResource);
              };


            var mi = typeof(T).GetMethods().FirstOrDefault(m => m.IsPublic && m.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant() && methodPredicate(m));

            if (mi == null) throw new InvalidOperationException();

            var state = await Get();
            ParameterExpression resourceParam = Expression.Parameter(typeof(TResource));
            MethodCallExpression methodCall = Expression.Call(Expression.Constant(state), mi, resourceParam);
            ParameterExpression stateParam = Expression.Parameter(typeof(T));
            var lambda = Expression.Lambda<Func<T, TResource, Result<TResource>>>(methodCall, stateParam, resourceParam).Compile();

            return lambda(state, resource);
        }
    }
}
