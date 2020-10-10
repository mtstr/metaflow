using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;

namespace Metaflow.Orleans
{
    public class RestfulGrain<T> : JournaledGrain<GrainState<T>>, IRestfulGrain<T>, ICustomStorageInterface<GrainState<T>, object>
    {
        private readonly IDispatcher<T> _dispatcher;
        private readonly IQuerySync<T> _querySync;
        private readonly EventStoreClient _eventStore;
        private readonly ILogger<RestfulGrain<T>> _logger;
        private readonly ITelemetryClient _telemetry;
        private string _stream;
        private IEventSerializer _eventSerializer;

        public RestfulGrain(IDispatcher<T> dispatcher, EventStoreClient eventStore, ILogger<RestfulGrain<T>> logger,
            ITelemetryClient telemetry, IEventSerializer eventSerializer, IQuerySync<T> querySync = null)
        {
            _dispatcher = dispatcher;
            _eventStore = eventStore;
            _logger = logger;
            _telemetry = telemetry;
            _eventSerializer = eventSerializer;
            _querySync = querySync;
        }

        public Task<bool> Exists() => Task.FromResult(State.Exists);

        public Task<int> GetVersion() => Task.FromResult(base.Version);

        public Task<T> Get()
        {
            return Task.FromResult(State.Value);
        }

        public override async Task OnDeactivateAsync()
        {
            // await ConfirmEvents();
            await base.OnDeactivateAsync();
        }

        public override async Task OnActivateAsync()
        {
            //     _stream = $"{typeof(T).Name}:{this.GetPrimaryKeyString()}";

            //     var stream = _eventStore.ReadStreamAsync(
            //         Direction.Forwards,
            //         _stream,
            //         StreamPosition.Start);

            //     if (await stream.ReadState != ReadState.StreamNotFound)
            //     {
            //         await foreach (var resolvedEvent in stream)
            //         {
            //             var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

            //             object eventObj = _eventSerializer.Deserialize(typeof(T), resolvedEvent.Event.EventType, json);

            //             if (eventObj != null) base.RaiseEvent(eventObj);
            //         }
            //     }

            //     await ConfirmEvents();
            await base.OnActivateAsync();
        }

        public Task<Result> Put<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.PUT, resource);
        }

        public Task<Result> Delete()
        {
            return Handle<T, T>(MutationRequest.DELETE, default);
        }

        public Task<Result> Delete<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.DELETE, resource);
        }

        public Task<Result> Patch<TDelta>(TDelta delta)
        {
            return Handle<T, TDelta>(MutationRequest.PATCH, delta);
        }

        public Task<Result> Post<TResource>(TResource resource)
        {
            return Handle<TResource, TResource>(MutationRequest.POST, resource);
        }


        public Task<Result> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request)
        {
            return Handle<TResource, TInput>(request.Request, request.Input);
        }

        protected override void TransitionState(GrainState<T> state, object @event)
        {
            if (@event is EventDto dto)
            {
                GrainState<T> newState = state.Apply(dto.Event);

                State.Exists = newState.Exists;
                State.Value = newState.Value;
            }
        }

        private async Task<Result> Handle<TResource, TInput>(MutationRequest request, TInput input)
        {
            _telemetry.TrackRequest<TResource, TInput>(request, GetPrimaryKeyString());

            if (!CreateRequest(request, input) && !State.Exists && !ImplicitCreateAllowed())
            {
                return NotFound<TResource, TInput>();
            }

            try
            {
                return Result.Ok(await HandleEvent<TResource, TInput>(request, input));
            }
            catch (Exception ex)
            {
                await HandleException<TResource, TInput>(request, input, ex);

                throw ex;
            }
        }

        private bool CreateRequest(MutationRequest request, object input)
        {
            return request == MutationRequest.POST && input is T;
        }

        private async Task<IEnumerable<object>> HandleEvent<TResource, TInput>(MutationRequest request, TInput input)
        {
            var reception = new Received<TInput>(request, typeof(TResource).Name, input);

            await Persist(new EventDto() { Name = $"Received:{typeof(TInput).Name}", Event = reception });

            if (!CreateRequest(request, input) && !State.Exists && ImplicitCreateAllowed())
            {
                var created = IsPostDefined()
                    ? (await Dispatch<T, T>(MutationRequest.POST, default))
                    : DefaultCreationEvent();

                await Persist<TResource, TInput>(created);
            }

            IEnumerable<object> events = await Dispatch<TResource, TInput>(request, input);

            var handleEvent = events.ToList();
            await Persist<TResource, TInput>(handleEvent);

            await UpdateQueryStore();

            _telemetry.TrackEvents<TResource, TInput>(GetPrimaryKeyString(), handleEvent);

            return handleEvent;
        }

        private Task UpdateQueryStore()
        {
            if (_querySync != null)
                return _querySync.UpdateQueryStore(GetPrimaryKeyString(), State.Value);

            return Task.CompletedTask;
        }

        private bool IsPostDefined()
        {
            return typeof(T).SelfMethod(MutationRequest.POST) != null;
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

        private async Task<IEnumerable<object>> Dispatch<TResource, TInput>(MutationRequest request, TInput input)
        {
            return await _dispatcher.Invoke<TResource, TInput>(State.Value, request, input);
        }

        private async Task HandleException<TResource, TInput>(MutationRequest request, TInput input, Exception ex)
        {
            var failure = new Failed<TInput>(request, typeof(TResource).Name, input, ex.Message);

            _logger.LogError(30001, ex, "Exception in event handling");

            _telemetry.TrackException<TResource, TInput>(request, GetPrimaryKeyString(), ex);

            await Persist(new List<EventDto>()
                {new EventDto() {Name = $"Failed:{typeof(TInput).Name}", Event = failure}});
        }

        private Result NotFound<TResource, TInput>()
        {
            var result =
                Result.Nok($"Object {GetPrimaryKeyString()} does not exist and does not support implicit creation");

            return result;
        }


        private static bool ImplicitCreateAllowed()
        {
            return typeof(T).GetCustomAttributes().OfType<RestfulAttribute>()
                .FirstOrDefault()?.AllowImplicitCreate ?? false;
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

        private string GetPrimaryKeyString()
        {
            return GrainReference.GrainIdentity.PrimaryKeyString;
        }

        public async Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage()
        {
            _stream = $"{typeof(T).Name}:{this.GetPrimaryKeyString()}";

            var stream = _eventStore.ReadStreamAsync(
                Direction.Forwards,
                _stream,
                StreamPosition.Start);

            var state = new GrainState<T>();
            var version = 0;

            if (await stream.ReadState != ReadState.StreamNotFound)
            {
                await foreach (var resolvedEvent in stream)
                {
                    var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);

                    object eventObj = _eventSerializer.Deserialize(typeof(T), resolvedEvent.Event.EventType, json);

                    if (eventObj != null)
                    {
                        state = state.Apply(eventObj);
                        version++;
                    }
                }

            }

            return new KeyValuePair<int, GrainState<T>>(version, state);
        }

        public async Task<bool> ApplyUpdatesToStorage(IReadOnlyList<object> updates, int expectedversion)
        {
            try
            {
                var enumerable = updates.OfType<EventDto>().ToList();

                var events = enumerable.Select(ed =>
                {
                    var json = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(ed.Event, options: new JsonSerializerOptions().Configure()));

                    return new EventData(Uuid.NewUuid(), ed.Name, json);
                }).ToList();

                await _eventStore.AppendToStreamAsync(_stream, expectedversion == 0 ? StreamRevision.None : Convert.ToUInt64(expectedversion - 1), events);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class EventDto
    {
        public string Name { get; set; }
        public object Event { get; set; }
    }

    public interface IEventSerializer
    {
        object Deserialize(Type type, string eventType, string json);
    }

    class EventSerializer : IEventSerializer
    {
        private static Type ResolvePropertyType(PropertyInfo pi)
        {
            var t = pi.PropertyType;
            if (t.IsGenericType && t.IsAssignableTo(typeof(IEnumerable)))
            {
                return t.GetGenericArguments().First();
            }

            return t;
        }

        public object Deserialize(Type type, string eventType, string json)
        {
            var split = eventType.Split(":");
            var (action, data) = (split[0], split[1]);
            var ownedTypes = type.GetProperties().Select(ResolvePropertyType).ToList();
            var eventDataType = ownedTypes.FirstOrDefault(ot => ot.Name == data);
            var targetType = (action, data) switch
            {
                ("Created", _) when data == type.Name => typeof(Created<>).MakeGenericType(type),
                ("Created", _) when eventDataType != null => typeof(Created<>).MakeGenericType(eventDataType),
                ("Replaced", _) when eventDataType != null => typeof(Replaced<>).MakeGenericType(eventDataType),
                ("Deleted", _) when eventDataType != null => typeof(Deleted<>).MakeGenericType(eventDataType),
                ("Deleted", _) when data == type.Name => typeof(Created<>).MakeGenericType(type),
                _ => null
            };

            if (targetType != null)
            {
                var e = System.Text.Json.JsonSerializer.Deserialize(json, targetType, new JsonSerializerOptions().Configure());
                return e;
            }

            return null;
        }
    }
}