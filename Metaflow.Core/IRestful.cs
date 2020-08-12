using System.Threading.Tasks;

namespace Metaflow
{
    public interface IRestful<T>
    {
        Task<T> Get();
        Task<Result> Put<TResource>(TResource resource);
        Task<Result> Patch<TDelta>(TDelta delta);

        Task<Result> Delete();
        Task<Result> Delete<TResource>(TResource resource);
        Task<Result> Post<TResource>(TResource resource);
    }
}
