using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;
using static Metaflow.Json;
using static Metaflow.EventSourcing;

namespace Metaflow.Orleans
{
    public class StateGrain<T> : JournaledGrain<State<T>>, IStateGrain<T>,
        ICustomStorageInterface<State<T>, object>
    {
        private readonly IClusterClient _clusterClient;
        private readonly EventStoreClient _eventStore;
        private readonly ILogger<StateGrain<T>> _logger;
        private string _stream;
        private readonly IEventSerializer _eventSerializer;
        private readonly IEventStreamId<T> _eventStreamId;

        public StateGrain(
            EventStoreClient eventStore,
            ILogger<StateGrain<T>> logger,
            IEventSerializer eventSerializer,
            IEventStreamId<T> eventStreamId)
        {
            _eventStore = eventStore;
            _logger = logger;
            _eventSerializer = eventSerializer;
            _eventStreamId = eventStreamId;
        }

        public Task<object> GetState() => Task.FromResult((object)State.Value);
        public Task<bool> Exists()
        {
            return Task.FromResult(State.Exists);
        }

        public Task<int> GetVersion() => Task.FromResult(base.Version);

        public Task<T> Get()
        {
            return Task.FromResult(State.Value);
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
        }


        protected override void TransitionState(State<T> state, object @event)
        {
            if (@event is EventDto dto)
            {
                State<T> newState = state.Apply(dto.Event);

                State.Exists = newState.Exists;
                State.Value = newState.Value;
            }
        }

        private string GrainId()
        {
            return GrainReference.GrainIdentity.PrimaryKeyString;
        }

        public async Task<KeyValuePair<int, State<T>>> ReadStateFromStorage()
        {
            _stream = _eventStreamId.Get(GrainId());

            var stream = _eventStore.ReadStreamAsync(
                Direction.Forwards,
                _stream,
                StreamPosition.Start);

            var state = new State<T>();
            var version = 0;

            if (await stream.ReadState != ReadState.StreamNotFound)
            {
                await foreach (var resolvedEvent in stream)
                {
                    var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

                    try
                    {
                        var eventObj = _eventSerializer.Deserialize(typeof(T), resolvedEvent.Event.EventType, json);

                        state = state.Apply(eventObj);
                        version++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(5004, ex, ex.Message);
                    }
                }
            }

            return new KeyValuePair<int, State<T>>(version, state);
        }

        public static int ModelVersion => typeof(T).ModelVersion();


        public Task<bool> ApplyUpdatesToStorage(IReadOnlyList<object> updates, int expectedversion)
        {
            return Task.FromResult(true);
        }
    }
}