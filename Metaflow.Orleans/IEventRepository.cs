using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow.Orleans
{
    public interface IEventRepository
    {
        Task WriteSnapshot<T>(Snapshot<T> snapshot);
        Task<Snapshot<T>> ReadSnapshot<T>(string entityId, int etag);
        IAsyncEnumerable<Event> ReadEvents(string entityId, int etag);
        Task<int> LatestVersion(string entityId);
        Task WriteEvent(Event @event);
    }
}
