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

    public class RestfulGrain<T> : JournaledGrain<State<T>>, IStateGrain<T>,
        ICustomStorageInterface<State<T>, object>
    {
        private readonly IDispatcher<T> _dispatcher;
        private readonly IClusterClient _clusterClient;
        private readonly UpgradeMap _upgradeMap;
        private readonly EventStoreClient _eventStore;
        private readonly ILogger<RestfulGrain<T>> _logger;
        private readonly ITelemetryClient _telemetry;
        private string _stream;
        private readonly IEventSerializer _eventSerializer;

        public RestfulGrain(IDispatcher<T> dispatcher, EventStoreClient eventStore, ILogger<RestfulGrain<T>> logger,
            ITelemetryClient telemetry, IEventSerializer eventSerializer, IClusterClient clusterClient,
            UpgradeMap upgradeMap)
        {
            _dispatcher = dispatcher;
            _eventStore = eventStore;
            _logger = logger;
            _telemetry = telemetry;
            _eventSerializer = eventSerializer;
            _clusterClient = clusterClient;
            _upgradeMap = upgradeMap;
        }

        public Task<object> GetState() => Task.FromResult((object)State.Value);
        public async Task<bool> Exists()
        {
            if (ModelVersion() == 1) return State.Exists;

            if (!State.Exists) await Upgrade();

            return State.Exists;
        }

        public Task<int> GetVersion() => Task.FromResult(base.Version);

        public async Task<T> Get()
        {
            if (ModelVersion() == 1) return State.Value;

            if (!State.Exists) await Upgrade();

            return State.Value;
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
        }


        private async Task<LegacyResult> Handle<TResource, TInput>(Operation request, TInput input)
        {
            _telemetry.TrackRequest<TResource, TInput>(request, GrainId());

            if (!CreateRequest(request, input) && !State.Exists && !ImplicitCreateAllowed())
            {
                return NotFound<TResource, TInput>();
            }

            try
            {
                var events = await HandleEvent<TResource, TInput>(request, input);
                var success = !events.Any(e => e is Rejected<TInput>);
                return success ? LegacyResult.Ok(events) : LegacyResult.Nok(events);
            }
            catch (Exception ex)
            {
                await HandleException<TResource, TInput>(request, input, ex);

                throw;
            }
        }

        private bool CreateRequest(Operation request, object input)
        {
            return request == Operation.POST && input is T;
        }

        private async Task<IEnumerable<object>> HandleEvent<TResource, TInput>(Operation request, TInput input)
        {
            var reception = new Received<TInput>(request, typeof(TResource).Name, input);

            await Persist(new EventDto() { Name = $"Received:{typeof(TInput).Name}", Event = reception });

            if (!State.Exists && !CreateRequest(request, input))
            {
                if (ModelVersion() == 1)
                {
                    if (ImplicitCreateAllowed())
                    {
                        IEnumerable<object> created = IsPostDefined()
                            ? (await Dispatch<T, T>(Operation.POST, default))
                            : DefaultCreationEvent();

                        await Persist<TResource, TInput>(created);
                    }
                }
                else
                {
                    await Upgrade();
                }
            }

            IEnumerable<object> events = await Dispatch<TResource, TInput>(request, input);

            List<object> handleEvent = events.ToList();

            await Persist<TResource, TInput>(handleEvent);

            _telemetry.TrackEvents<TResource, TInput>(GrainId(), handleEvent);

            return handleEvent;
        }

        private async Task Upgrade()
        {
            _logger.LogInformation("Entity upgrade triggered");

            Type baseType = _upgradeMap.For<T>();
            Type type = typeof(T);

            if (baseType != null)
            {
                if (_clusterClient.GetGrain(
                    typeof(IStateGrain<>).MakeGenericType(baseType),
                    GrainId()) is IStateGrain baseGrain && await baseGrain.Exists())
                {
                    var upgradedState = (T)type.GetMethod("Upgrade")
                        .Invoke(null, new[] { await baseGrain.GetState() });

                    var upgradeEvent = new List<object>() { new Upgraded<T>(upgradedState) };

                    await Persist<T, T>(upgradeEvent);
                }
            }
            else
            {
                _logger.LogWarning($"Upgrade base not found {type.Name} v{type.ModelVersion()}");
            }

        }

        private bool IsPostDefined()
        {
            return typeof(T).SelfMethod(Operation.POST) != null;
        }

        private IEnumerable<object> DefaultCreationEvent()
        {
            if (State.Value == null)
            {
                var initializer = typeof(T).GetMethods().FirstOrDefault(mi =>
                    mi.IsStatic && mi.IsPublic && mi.Name == "Init" && mi.GetParameters().Count() == 0 &&
                    mi.ReturnType == typeof(T));

                if (initializer == null)
                    throw new DispatchException($"No initializer method found for nullable type {typeof(T).Name}");
                try
                {
                    State.Value = (T)initializer.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    throw new DispatchException($"Failed to initialize value of {typeof(T).Name}", ex);
                }
            }

            return new List<object>() { new Created<T>(State.Value) };
        }

        private async Task<IEnumerable<object>> Dispatch<TResource, TInput>(Operation request, TInput input)
        {
            return await _dispatcher.Invoke<TResource, TInput>(State.Value, request, input);
        }

        private async Task HandleException<TResource, TInput>(Operation request, TInput input, Exception ex)
        {
            var failure = new Failed<TInput>(request, typeof(TResource).Name, input, ex.Message);

            _logger.LogError(30001, ex, "Exception in event handling");

            _telemetry.TrackException<TResource, TInput>(request, GrainId(), ex);

            await Persist(new List<EventDto>()
                {new EventDto() {Name = $"Failed:{typeof(TInput).Name}", Event = failure}});
        }

        private LegacyResult NotFound<TResource, TInput>()
        {
            var result =
                LegacyResult.Nok($"Object {GrainId()} does not exist and does not support implicit creation");

            return result;
        }


        private static bool ImplicitCreateAllowed()
        {
            return typeof(T).RestfulAttribute()?.AllowImplicitCreate ?? false;
        }

        private Task Persist<TResource, TInput>(IEnumerable<object> events)
        {
            return Persist(events.Select(e => new EventDto() { Name = e.Name<TResource, TInput>(), Event = e }));
        }

        private Task Persist(EventDto e)
        {
            return Persist(new List<EventDto> { e });
        }

        private async Task Persist(IEnumerable<EventDto> eventDtos)
        {
            base.RaiseEvents(eventDtos);

            await ConfirmEvents();
        }

        private string GrainId()
        {
            return GrainReference.GrainIdentity.PrimaryKeyString;
        }

        public async Task<KeyValuePair<int, State<T>>> ReadStateFromStorage()
        {
            _stream = $"{typeof(T).Name}:{ModelVersion()}:{this.GrainId()}";

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

        private int ModelVersion()
        {
            return typeof(T).ModelVersion();
        }


        public async Task<bool> ApplyUpdatesToStorage(IReadOnlyList<object> updates, int expectedversion)
        {
            try
            {
                List<EventDto> enumerable = updates.OfType<EventDto>().ToList();

                List<EventData> events = enumerable.Select(ed =>
                {
                    byte[] json = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(ed.Event,
                        options: new JsonSerializerOptions().Configure()));

                    return new EventData(Uuid.NewUuid(), ed.Name, json);
                }).ToList();

                await _eventStore.AppendToStreamAsync(_stream,
                    expectedversion == 0 ? StreamRevision.None : Convert.ToUInt64(expectedversion - 1), events);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(5003, ex, ex.Message);
            }
            return true;
        }
    }
}