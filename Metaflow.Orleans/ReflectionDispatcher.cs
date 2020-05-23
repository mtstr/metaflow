using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Metaflow
{
    public class ReflectionDispatcher<T> : IDispatcher<T>
    where T : class, new()
    {
        public virtual Task<Result<TResource>> Invoke<TResource, TInput>(T owner, MutationRequest request, TInput input)
        {
            if (request == MutationRequest.DELETE && typeof(T) == typeof(TInput))
            {
                return DispatchSelfDelete<TResource>(owner);
            }
            else
            {
                return Dispatch<TResource, TInput>(owner, request, input);
            }
        }

        private Task<Result<TResource>> DispatchSelfDelete<TResource>(T owner)
        {
            MethodInfo mi = typeof(T).DeleteSelfMethod();

            if (mi == null) throw new InvalidOperationException();

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, Result<TResource>> lambda = Expression.Lambda<Func<T, Result<TResource>>>(methodCall, stateParam).Compile();

            return Task.FromResult(lambda(owner));
        }


        private static Task<Result<TResource>> Dispatch<TResource, TInput>(T owner, MutationRequest request, TInput input)
        {
            MethodInfo mi = typeof(T).MatchingMethods(request, false).For(typeof(TResource), typeof(TInput));

            if (mi == null) throw new InvalidOperationException();

            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi, inputParam);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, TInput, Result<TResource>> lambda = Expression.Lambda<Func<T, TInput, Result<TResource>>>(methodCall, stateParam, inputParam).Compile();

            return Task.FromResult(lambda(owner, input));
        }
    }
}
