using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Orleans;
using static Metaflow.MutationRequest;
namespace Metaflow
{
    public class ReflectionDispatcher<T> : IDispatcher<T>

    {



        public virtual Task<IEnumerable<object>> Invoke<TResource, TInput>(T resourceOwner, MutationRequest request, TInput input)
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


        private Task<IEnumerable<object>> DispatchSelf<TResource>(T owner, MutationRequest request)
        {
            var mi = typeof(T).SelfMethod(request);

            if (mi == null) ThrowForMissingMethod(request);

            var methodCall = Expression.Call(Expression.Constant(owner), mi);

            var stateParam = Expression.Parameter(typeof(T));

            Func<T, IEnumerable<object>> lambda = Expression.Lambda<Func<T, IEnumerable<object>>>(methodCall, stateParam).Compile();

            return Task.FromResult(lambda(owner));
        }

        private static void ThrowForMissingMethod(MutationRequest request)
        {
            throw new InvalidOperationException($"No valid method for {request} found in {typeof(T).Name}");
        }

        private static Task<IEnumerable<object>> Dispatch<TResource, TInput>(T owner, MutationRequest request, TInput input)
        {
            var mi = typeof(T).MatchingMethods(request, false).For(typeof(TResource), typeof(TInput));

            if (mi == null) ThrowForMissingMethod(request);

            var inputParam = Expression.Parameter(typeof(TInput));

            var methodCall = Expression.Call(Expression.Constant(owner), mi, inputParam);

            var stateParam = Expression.Parameter(typeof(T));

            Func<T, TInput, IEnumerable<object>> lambda = Expression.Lambda<Func<T, TInput, IEnumerable<object>>>(methodCall, stateParam, inputParam).Compile();

            return Task.FromResult(lambda(owner, input));
        }
    }
}
