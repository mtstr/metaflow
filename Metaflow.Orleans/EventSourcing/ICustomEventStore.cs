using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow.Orleans
{
    public interface ICustomEventStore
    {
        Task WriteNewSnapshot<T>(int etag, GrainState<T> state);
        Task<bool> ApplyUpdatesToStorage(string entityId, IReadOnlyList<object> updates, int expectedversion);

        Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage<T>(string entityId, int? etag = null);
        Task<int> LatestSnapshotVersion(string entityId);
    }
}
