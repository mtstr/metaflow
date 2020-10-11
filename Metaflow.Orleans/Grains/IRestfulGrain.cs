using System.Threading.Tasks;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain
    {
        Task<object> GetState();
        Task<int> GetVersion();

        Task<bool> Exists();
    }

    public interface IRestfulGrain<T> : IRestful<T>, IRestfulGrain, IGrainWithStringKey
    {
        Task<Result> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request);

    }
}