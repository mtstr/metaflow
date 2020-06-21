using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow.Orleans
{
    public interface IEventRepository
    {
        Task WriteSnapshot<T>(Snapshot<T> snapshot);
        Task<Snapshot<T>> ReadSnapshot<T>(string entityId, int etag);
        IAsyncEnumerable<Event> ReadEvents(string entityId, int startingEtag, int? targetEtag = null);
        Task<int> LatestEventVersion(string entityId, int? etag = null);
        Task<int> LatestSnapshotVersion(string entityId, int? etag = null);
        Task WriteEvents(IEnumerable<Event> events);
        Task WriteEvent(Event @event);
    }
}
