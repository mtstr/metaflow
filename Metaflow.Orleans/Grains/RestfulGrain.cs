using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.EventSourcing;

namespace Metaflow.Orleans
{

    public class RestfulGrain<T> : JournaledGrain<GrainState<T>>, IRestfulGrain<T>
    {
        private const int SnapshotPeriodity = 20;

        private readonly IDispatcher<T> _dispatcher;
        private readonly ICustomEventStore _eventStore;
        private readonly ILogger<RestfulGrain<T>> _logger;
        private readonly ITelemetryClient _telemetry;
        private int _latestSnapshotVersion;

        public RestfulGrain(IDispatcher<T> dispatcher, ICustomEventStore eventStore, ILogger<RestfulGrain<T>> logger, ITelemetryClient telemetry)
        {
            _dispatcher = dispatcher;
            _eventStore = eventStore;
            _logger = logger;
            _telemetry = telemetry;
        }

        public Task<bool> Exists() => Task.FromResult(State.Exists);

        public Task<T> Get()
        {
            return Task.FromResult(State.Value);
        }

        public override async Task OnActivateAsync()
        {
            _latestSnapshotVersion = await _eventStore.LatestSnapshotVersion(GetPrimaryKeyString());

            await base.OnActivateAsync();
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

        public async Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage()
        {
            try
            {
                var state = await _eventStore.ReadStateFromStorage<T>(GetPrimaryKeyString());
                return state;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(50001, ex, "Critical event sourcing error");
                return new KeyValuePair<int, GrainState<T>>(0, new GrainState<T>());
            }
        }

        public Task<bool> ApplyUpdatesToStorage(IReadOnlyList<object> updates, int expectedversion)
        {
            try
            {
                return _eventStore.ApplyUpdatesToStorage(GetPrimaryKeyString(), updates, expectedversion);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(50002, ex, "Critical event sourcing error");
                return Task.FromResult(false);
            }
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
            _telemetry.TrackRequest<TResource, TInput>(request, GetPrimaryKeyString());

            if (!State.Exists && !ImplicitCreateAllowed())
            {
                return NotFound<TResource, TInput>();
            }

            try
            {
                return await HandleEvent<TResource, TInput>(request, input);
            }
            catch (Exception ex)
            {
                await HandleException<TResource, TInput>(request, input, ex);

                throw ex;
            }
        }

        private async Task<Result<TResource>> HandleEvent<TResource, TInput>(MutationRequest request, TInput input)
        {
            var reception = new Received<TResource, TInput>(request, input);

            await Persist(reception);

            if (!State.Exists && ImplicitCreateAllowed())
            {
                await Persist(Result<T>.Created(State.Value).AsEvent(request, input));
            }

            Result<TResource> result = await Dispatch<TResource, TInput>(request, input);

            var @event = result.AsEvent(request, input);

            await Persist(@event);

            await Snapshot();

            _telemetry.TrackResult<TResource, TInput>(GetPrimaryKeyString(), result);

            return result;
        }

        private async Task<Result<TResource>> Dispatch<TResource, TInput>(MutationRequest request, TInput input)
        {
            return await _dispatcher.Invoke<TResource, TInput>(State.Value, request, input);
        }

        private async Task HandleException<TResource, TInput>(MutationRequest request, TInput input, Exception ex)
        {
            var failure = new Failed<TResource, TInput>(request, input, ex.Message);

            _logger.LogError(30001, ex, "Exception in event handling");

            _telemetry.TrackException<TResource, TInput>(request, GetPrimaryKeyString(), ex);

            await Persist(failure);
        }

        private Result<TResource> NotFound<TResource, TInput>()
        {
            var result = Result<TResource>.Nok($"Object {GetPrimaryKeyString()} does not exist and cannot be created implicitly");

            _telemetry.TrackResult<TResource, TInput>(GetPrimaryKeyString(), result);

            return result;
        }

        private async Task Snapshot()
        {
            if (TimeForNewSnapshot())
            {
                await _eventStore.WriteNewSnapshot(Version, State);
                _latestSnapshotVersion = Version;
            }
        }

        private static bool ImplicitCreateAllowed()
        {
            return typeof(T).GetCustomAttributes().OfType<RestfulAttribute>()
                                .FirstOrDefault()?.AllowImplicitCreate ?? false;
        }

        private async Task Persist(object @event)
        {
            base.RaiseEvent(@event);
            await ConfirmEvents();
        }

        private string GetPrimaryKeyString()
        {
            return GrainReference.GrainIdentity.PrimaryKeyString;
        }
    }
}
