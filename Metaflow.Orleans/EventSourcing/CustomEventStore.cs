using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.EventSourcing.CustomStorage;

namespace Metaflow.Orleans
{

    public class CustomEventStore : ICustomEventStore
    {
        private readonly ILogger<CustomEventStore> _log;
        private readonly IEventRepository _eventRepository;
        private string _entityId;

        public CustomEventStore(ILogger<CustomEventStore> log, IEventRepository eventRepository)
        {
            _log = log;
            _eventRepository = eventRepository;
        }

        public async Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage<T>(string entityId, int? etag = null)
        {
            _entityId = entityId;

            _log.LogInformation("ReadStateFromStorage: start");

            int desiredVersion = etag ?? await GetVer();

            GrainState<T> state = new GrainState<T>();

            var latestSuitableSnapshotVersion = await _eventRepository.LatestSnapshotVersion(entityId, etag);

            if (latestSuitableSnapshotVersion > 0)
            {
                Snapshot<T> snapshot = await _eventRepository.ReadSnapshot<T>(_entityId, latestSuitableSnapshotVersion);

                _log.LogInformation($"ReadStateFromStorage: ReadSnapshot loaded etag {latestSuitableSnapshotVersion}");

                state = snapshot.State;
            }

            if (latestSuitableSnapshotVersion < desiredVersion)
            {
                await ApplyNewerEvents(latestSuitableSnapshotVersion, state, desiredVersion);
            }

            _log.LogInformation($"ReadStateFromStorage: returning etag {desiredVersion}");

            return new KeyValuePair<int, GrainState<T>>(desiredVersion, state);
        }

        public async Task<bool> ApplyUpdatesToStorage(string entityId, IReadOnlyList<object> updates, int expectedversion)
        {
            _log.LogInformation($"ApplyUpdatesToStorage: start, expected etag {expectedversion}, update count {updates.Count}");


            _log.LogInformation("ApplyUpdatesToStorage: checking persisted stream version");

            int ver = await GetVer();

            _log.LogInformation($"ApplyUpdatesToStorage: persisted version {ver} is expected? {ver == expectedversion}");

            if (ver != expectedversion) return false;

            await WriteEvents(ver, updates);

            return true;
        }

        private async Task WriteEvents(int etag, IReadOnlyCollection<object> updates)
        {
            List<Event> events = new List<Event>();

            foreach (var u in updates)
            {
                etag++;
                bool success = u.GetType().GetGenericTypeDefinition() != typeof(Failed<,>) &&
                            u.GetType().GetGenericTypeDefinition() != typeof(Rejected<,>);

                var e = new Event(_entityId, success, etag, payload: u);

                await _eventRepository.WriteEvent(e);
            }
        }

        private Task<int> GetVer()
        {
            return _eventRepository.LatestEventVersion(_entityId);
        }

        private async Task<int> ApplyNewerEvents<T>(int snapshotETag, GrainState<T> state, int? targetEtag = null)
        {
            int etag = snapshotETag;

            IAsyncEnumerable<Event> events = GetEvents(snapshotETag, targetEtag);

            await foreach (var @event in events)
            {
                etag = @event.ETag;
                state = state.Apply(@event.Payload);
            }

            _log.LogInformation($"ApplyNewerEvents: exit returning etag {etag}");

            return etag;
        }

        private IAsyncEnumerable<Event> GetEvents(int snapshotETag, int? targetEtag = null)
        {
            return _eventRepository.ReadEvents(_entityId, snapshotETag, targetEtag);
        }

        public async Task WriteNewSnapshot<T>(int etag, GrainState<T> state)
        {
            _log.LogInformation($"WriteNewSnapshot: start write for etag {etag}");

            await _eventRepository.WriteSnapshot(new Snapshot<T> { EntityId = _entityId, ETag = etag, State = state });

            _log.LogInformation("WriteNewSnapshot: exit");
        }

        public Task<int> LatestSnapshotVersion(string entityId)
        {
            return _eventRepository.LatestSnapshotVersion(entityId);
        }
    }
}
