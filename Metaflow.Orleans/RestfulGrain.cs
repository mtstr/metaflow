using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Orleans.EventSourcing;

namespace Metaflow.Orleans
{

    public class GrainState<T>
    {
        public T Value { get; internal set; }
    }

    public class RestfulGrain<T> : JournaledGrain<GrainState<T>>, IRestfulGrain<T>
    {
        private readonly IDispatcher<T> _dispatcher;

        public RestfulGrain(IDispatcher<T> dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public Task<T> Get()
        {
            return Task.FromResult(State.Value);
        }

        protected override void TransitionState(GrainState<T> state, object @event)
        {
            Func<MethodInfo, Type, bool> match = (m, t) =>
            {
                var p = m.GetParameters().ToList();
                return m.IsPublic && m.Name == "Apply" && m.ReturnType == typeof(T) && p.Count == 1 && p[0].ParameterType == t;
            };
            var mi = typeof(T).GetMethods().FirstOrDefault(m => match(m, @event.GetType()));

            if (mi != null)
            {
                State.Value = (T)mi.Invoke(state.Value, new[] { @event });
            }
        }


        private async Task<Result<TResource>> Handle<TResource, TInput>(MutationRequest request, TInput input)
        {
            RaiseEvent(new Received<TResource, TInput>(request, input));
            await ConfirmEvents();

            Result<TResource> result;
            object @event;

            try
            {
                result = await _dispatcher.Invoke<TResource, TInput>(State.Value, request, input);

                @event = result.OK switch
                {
                    true => Succeeded<TResource>(request, result),
                    false => new Rejected<TResource, TInput>(request, input)
                };
            }
            catch (Exception ex)
            {
                result = Result<TResource>.Nok(ex.Message);
                @event = new Failed<TResource, TInput>(request, input, ex);
            }

            RaiseEvent(@event);
            await ConfirmEvents();

            return result;
        }

        private object Succeeded<TResource>(MutationRequest request, Result<TResource> result)
        {
            return result.StateChange switch
            {
                StateChange.Created => new Created<TResource>(result.After),
                StateChange.Replaced => new Replaced<TResource>(result.Before, result.After),
                StateChange.Deleted => new Deleted<TResource>(result.Before),
                StateChange.Updated => new Updated<TResource>(result.Before, result.After),
                _ => throw new InvalidStateChange(request, result)
            };
        }

        public Task<Result<TResource>> Put<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.PUT, resource);
        }

        public Task<Result<T>> Delete()
        {
            return Handle<T, T>(MutationRequest.DELETE, default);

        }

        public Task<Result<TResource>> Delete<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.DELETE, resource);
        }

        public Task<Result<TResource>> Post<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.POST, resource);
        }

        public Task<Result<TResource>> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request)
        {
            return Handle<TResource, TInput>(request.Request, request.Input);
        }
    }
}
