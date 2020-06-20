using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow.Orleans
{
    public interface ICustomEventStore
    {
        Task<bool> ApplyUpdatesToStorage(string entityId, IReadOnlyList<object> updates, int expectedversion);
        Task<KeyValuePair<int, GrainState<T>>> ReadStateFromStorage<T>(string entityId, int? baseEtag = null);
    }
}
