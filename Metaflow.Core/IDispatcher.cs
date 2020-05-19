using System.Threading.Tasks;

namespace Metaflow
{
    public interface IDispatcher<T>
    {
        Task<Result<TResource>> Invoke<TResource>(T owner, MutationRequest request, TResource resource) where TResource : class, new();
    }
}
