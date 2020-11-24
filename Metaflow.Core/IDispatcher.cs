using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metaflow
{
    public interface IDispatcher<T>
    {
        Task<IEnumerable<object>> Invoke<TResource, TInput>(T owner, MutationRequest request, TInput input);

    }
}
