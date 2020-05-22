using System.Threading.Tasks;

namespace Metaflow
{
    public interface IRestful<T>
    {
        Task<T> Get();
        Task<Result<TResource>> Put<TResource>(TResource resource);
        Task<Result<T>> Delete();
        Task<Result<TResource>> Delete<TResource>(TResource resource);
        Task<Result<TResource>> Post<TResource>(TResource resource);
    }
}
