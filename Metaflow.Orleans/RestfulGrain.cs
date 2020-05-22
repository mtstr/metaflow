using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Orleans.EventSourcing;

namespace Metaflow.Orleans
{
    public class RestfulGrain<T> : JournaledGrain<T>, IRestfulGrain<T>
    where T : class, new()
    {
        private readonly IDispatcher<T> _dispatcher;

        public RestfulGrain(IDispatcher<T> dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public T _readOnlyState;

        public Task<T> Get()
        {
            if (_readOnlyState == null) UpdateReadOnlyState();

            return Task.FromResult(_readOnlyState);
        }

        private void UpdateReadOnlyState()
        {
            if (State == null) _readOnlyState = null;
            _readOnlyState = State.Copy();
        }

        private async Task<Result<TResource>> Handle<TResource>(MutationRequest request, TResource resource)
        {
            RaiseEvent(new Received<TResource>(request, resource));
            await ConfirmEvents();

            Result<TResource> result;
            object @event;

            try
            {
                result = await _dispatcher.Invoke<TResource>(State, request, resource);

                @event = result.OK switch
                {
                    true => Succeeded<TResource>(request, result),
                    false => new Rejected<TResource>(request, resource)
                };
            }
            catch (Exception ex)
            {
                result = Result<TResource>.Nok(ex.Message);
                @event = new Failed<TResource>(request, resource, ex);
            }

            RaiseEvent(@event);
            await ConfirmEvents();
            UpdateReadOnlyState();

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
            return Handle<TResource>(MutationRequest.PUT, resource);
        }

        public Task<Result<T>> Delete()
        {
            return Handle<T>(MutationRequest.DELETE, null);

        }

        public Task<Result<TResource>> Delete<TResource>(TResource resource)
        {
            return Handle<TResource>(MutationRequest.DELETE, resource);
        }

        public Task<Result<TResource>> Post<TResource>(TResource resource)
        {
            return Handle<TResource>(MutationRequest.POST, resource);
        }
    }
}
