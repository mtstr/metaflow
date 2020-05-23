using System.Threading.Tasks;

namespace Metaflow
{
    public interface IDispatcher<T>
    {
        Task<Result<TResource>> Invoke<TResource,TInput>(T owner, MutationRequest request, TInput input) ;
    }
}
