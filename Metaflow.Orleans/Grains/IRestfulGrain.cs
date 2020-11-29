using System.Threading.Tasks;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IStateGrain
    {
        Task<object> GetState();
        Task<int> GetVersion();

        Task<bool> Exists();
    }

    public interface IStateGrain<T> : IStateGrain, IGrainWithStringKey
    {
    }
}