using System.Threading.Tasks;

namespace Metaflow
{
    public interface IRestful<T> 
    {
        Task<T> Get();
        Task<Result<TResource>> Handle<TResource>(MutationRequest request, TResource resource) where TResource : class, new();
    }
}
