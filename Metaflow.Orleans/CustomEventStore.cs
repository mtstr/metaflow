using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.EventSourcing.CustomStorage;

namespace Metaflow.Orleans
{

    public class CustomEventStore : ICustomEventStore
    {
        private const int SnapshotPeriodity = 5;
        private readonly ILogger<CustomEventStore> _log;
        private readonly IEventRepository _eventRepository;
        private string _entityId;

        public CustomEventStore(ILogger<CustomEventStore> log, IEventRepository eventRepository)
        {
            _log = log;
            _eventRepository = eventRepository;
        }

        public async Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage<T>(string entityId, int? version = null)
        {
            _entityId = entityId;

            _log.LogInformation("ReadStateFromStorage: start");

            int etag = version ?? await GetVer();

            Snapshot<T> snapshot = await _eventRepository.ReadSnapshot<T>(_entityId, etag);
            T state = snapshot.State;

            _log.LogInformation($"ReadStateFromStorage: ReadSnapshot loaded etag {etag}");

            int newETag = await ApplyNewerEvents(etag, state);

            if (TimeForNewSnapshot(etag, newETag)) await WriteNewSnapshot(newETag, state);

            etag = newETag;

            _log.LogInformation($"ReadStateFromStorage: returning etag {etag}");

            return new KeyValuePair<int, GrainState<T>>(etag, new GrainState<T> { Value = state });
        }

        private static bool TimeForNewSnapshot(int etag, int newETag)
        {
            return newETag != etag && (newETag - etag) % SnapshotPeriodity == 0;
        }

        public async Task<bool> ApplyUpdatesToStorage(string entityId, IReadOnlyList<object> updates, int expectedversion)
        {
            _log.LogInformation($"ApplyUpdatesToStorage: start, expected etag {expectedversion}, update count {updates.Count}");


            _log.LogInformation("ApplyUpdatesToStorage: checking persisted stream version");

            int ver = await GetVer();

            _log.LogInformation($"ApplyUpdatesToStorage: persisted version {ver} is expected? {ver == expectedversion}");

            if (ver != expectedversion) return false;

            // if (ver == 0)
            // {
            //     _log.LogInformation("ApplyUpdatesToStorage: etag 0 special-case write Initialized event");

            //     await _eventRepository.WriteEvent(new Initialized
            //     {
            //         ETag = 0,
            //         CustomerId = GrainPrimaryKey
            //     });

            //     _log.LogInformation("ApplyUpdatesToStorage: etag 0 special-case write snapshot");

            //     await WriteNewSnapshot(0, State);
            // }

            foreach (var e in updates)
            {
                ver++;

                _log.LogInformation($"ApplyUpdatesToStorage: update ver {ver} event {e.GetType().Name}");

                await WriteEvent(ver, e);
            }

            return true;
        }

        private Task WriteEvent(int ver, object e)
        {
            return _eventRepository.WriteEvent(new Event() { EntityId = _entityId, ETag = ver, Payload = e });
        }

        private Task<int> GetVer()
        {
            return _eventRepository.LatestVersion(_entityId);
        }

        private async Task<int> ApplyNewerEvents<T>(int snapshotETag, T state)
        {
            int etag = snapshotETag;

            IAsyncEnumerable<Event> events = GetEvents(snapshotETag);

            await foreach (var @event in events)
            {
                etag = @event.ETag;
                state = state.Apply(@event).Value;
            }

            _log.LogInformation($"ApplyNewerEvents: exit returning etag {etag}");

            return etag;
        }

        private IAsyncEnumerable<Event> GetEvents(int snapshotETag)
        {
            return _eventRepository.ReadEvents(_entityId, snapshotETag);
        }

        private async Task WriteNewSnapshot<T>(int etag, T state)
        {
            _log.LogInformation($"WriteNewSnapshot: start write for etag {etag}");

            await _eventRepository.WriteSnapshot(new Snapshot<T> { EntityId = _entityId, ETag = etag, State = state });

            _log.LogInformation("WriteNewSnapshot: exit");
        }
    }
}
