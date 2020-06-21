using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Orleans.EventSourcing;

namespace Metaflow.Orleans
{

    public class RestfulGrain<T> : JournaledGrain<GrainState<T>>, IRestfulGrain<T>
    {
        private const int SnapshotPeriodity = 20;

        private readonly IDispatcher<T> _dispatcher;
        private readonly ICustomEventStore _eventStore;

        private int _latestSnapshotVersion = 0;

        public RestfulGrain(IDispatcher<T> dispatcher, ICustomEventStore eventStore)
        {
            _dispatcher = dispatcher;
            _eventStore = eventStore;
        }

        public Task<bool> Exists() => Task.FromResult(State.Exists);

        public Task<T> Get()
        {
            return Task.FromResult(State.Value);
        }

        public override async Task OnActivateAsync()
        {
            _latestSnapshotVersion = await _eventStore.LatestSnapshotVersion(GetPrimaryKeyString());
        }

        protected override void TransitionState(GrainState<T> state, object @event)
        {
            GrainState<T> newState = state.Apply(@event);

            State.Exists = newState.Exists;
            State.Value = newState.Value;
        }


        private bool TimeForNewSnapshot()
        {
            return Version - _latestSnapshotVersion >= SnapshotPeriodity;
        }

        private async Task<Result<TResource>> Handle<TResource, TInput>(MutationRequest request, TInput input)
        {
            var reception = new Received<TResource, TInput>(request, input);
            await LogEvent(reception);

            Result<TResource> result;
            object @event;

            try
            {
                result = await _dispatcher.Invoke<TResource, TInput>(State.Value, request, input);

                @event = result.OK ?
                                Succeeded(request, input, result) :
                                new Rejected<TResource, TInput>(request, input, result.Reason);
            }
            catch (Exception ex)
            {
                result = Result<TResource>.Nok(ex.Message);
                @event = new Failed<TResource, TInput>(request, input, ex);
            }

            await LogEvent(@event);

            if (TimeForNewSnapshot())
            {
                await _eventStore.WriteNewSnapshot(Version, State);
                _latestSnapshotVersion = Version;
            }

            return result;
        }

        private async Task LogEvent(object event1)
        {
            base.RaiseEvent(event1);
            await ConfirmEvents();
        }

        private object Succeeded<TResource, TInput>(MutationRequest request, TInput input, Result<TResource> result)
        {
            return result.StateChange switch
            {
                StateChange.Created => new Created<TResource>(result.After),
                StateChange.Replaced => new Replaced<TResource>(result.Before, result.After),
                StateChange.Deleted => new Deleted<TResource>(result.Before),
                StateChange.Updated => new Updated<TResource>(result.Before, result.After),
                StateChange.None => new Ignored<TResource, TInput>(request, input),
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

        public Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage()
        {
            return _eventStore.ReadStateFromStorage<T>(GetPrimaryKeyString());
        }

        private string GetPrimaryKeyString()
        {
            return GrainReference.GrainIdentity.PrimaryKeyString;
        }

        public Task<bool> ApplyUpdatesToStorage(IReadOnlyList<object> updates, int expectedversion)
        {
            return _eventStore.ApplyUpdatesToStorage(GetPrimaryKeyString(), updates, expectedversion);
        }
    }
}
