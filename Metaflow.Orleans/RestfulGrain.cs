using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Orleans;
using Orleans.EventSourcing;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain<T> : IRestful<T>, IGrainWithStringKey
    {

    }

    public class RestfulGrain<T> : JournaledGrain<T>, IRestfulGrain<T>
    where T : class, new()
    {

        public Task<T> Get() => Task.FromResult(State);

        public async Task<Result<TResource>> Handle<TResource>(MutationRequest request, TResource resource) where TResource : class, new()
        {
            RaiseEvent(new Received<TResource>(request, resource));

            await ConfirmEvents();

            Func<MethodInfo, bool> methodPredicate = (MethodInfo mi) =>
              {
                  var p = mi.GetParameters().ToList();
                  return p.Count == 1 && p[0].ParameterType == typeof(TResource);
              };


            var mi = typeof(T).GetMethods().FirstOrDefault(m => m.IsPublic && m.Name.ToUpperInvariant() == request.ToString().ToUpperInvariant() && methodPredicate(m));

            if (mi == null) throw new InvalidOperationException();

            Result<TResource> result;

            try
            {
                ParameterExpression resourceParam = Expression.Parameter(typeof(TResource));
                MethodCallExpression methodCall = Expression.Call(Expression.Constant(State), mi, resourceParam);
                ParameterExpression stateParam = Expression.Parameter(typeof(T));
                var lambda = Expression.Lambda<Func<T, TResource, Result<TResource>>>(methodCall, stateParam, resourceParam).Compile();

                result = lambda(State, resource);

                if (result.OK)
                    RaiseEvent(Succeeded<TResource>(request, result, resource));
                else
                    RaiseEvent(new Rejected<TResource>(request, resource));
            }
            catch (Exception ex)
            {
                RaiseEvent(new Failed<TResource>(request, resource, ex));
                result = Result<TResource>.Nok(ex.Message);
            }

            return result;
        }

        private object Succeeded<TResource>(MutationRequest request, Result<TResource> result, TResource resource) where TResource : class, new()
        {
            return result.StateChange switch
            {
                StateChange.Created => new Created<TResource>(result.After),
                StateChange.Replaced => new Replaced<TResource>(result.Before, result.After),
                StateChange.Deleted => new Deleted<TResource>(result.Before),
                StateChange.Updated => new Updated<TResource>(result.Before, result.After),
                _ => throw new InvalidStateChange(request, result, resource)
            };
        }
    }
}
