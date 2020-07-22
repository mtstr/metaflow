using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using static Metaflow.MutationRequest;
namespace Metaflow
{
    public class ReflectionDispatcher<T> : IDispatcher<T>

    {
        public virtual Task<Result<TResource>> Invoke<TResource, TInput>(T resourceOwner, MutationRequest request, TInput input)
        {
            static bool implicitCreation() => typeof(TResource) == typeof(T) && typeof(TInput) == typeof(T);

            return request switch
            {
                DELETE when input is T _ => DispatchSelf<TResource>(resourceOwner, request),
                POST when input is T self => DispatchSelf<TResource>(self, request),
                POST when implicitCreation() => DispatchSelf<TResource>(resourceOwner, request),
                _ => Dispatch<TResource, TInput>(resourceOwner, request, input)
            };
        }

        private Task<Result<TResource>> DispatchSelf<TResource>(T owner, MutationRequest request)
        {
            MethodInfo mi = typeof(T).SelfMethod(request);

            if (mi == null) ThrowForMissingMethod(request);

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, Result<TResource>> lambda = Expression.Lambda<Func<T, Result<TResource>>>(methodCall, stateParam).Compile();

            return Task.FromResult(lambda(owner));
        }

        private static void ThrowForMissingMethod(MutationRequest request)
        {
            throw new InvalidOperationException($"No valid method for {request} found in {typeof(T).Name}");
        }

        private static Task<Result<TResource>> Dispatch<TResource, TInput>(T owner, MutationRequest request, TInput input)
        {
            MethodInfo mi = typeof(T).MatchingMethods(request, false).For(typeof(TResource), typeof(TInput));

            if (mi == null) ThrowForMissingMethod(request);

            ParameterExpression inputParam = Expression.Parameter(typeof(TInput));

            MethodCallExpression methodCall = Expression.Call(Expression.Constant(owner), mi, inputParam);

            ParameterExpression stateParam = Expression.Parameter(typeof(T));

            Func<T, TInput, Result<TResource>> lambda = Expression.Lambda<Func<T, TInput, Result<TResource>>>(methodCall, stateParam, inputParam).Compile();

            return Task.FromResult(lambda(owner, input));
        }
    }
}
