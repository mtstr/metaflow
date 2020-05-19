using Orleans;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain<T> : IRestful<T>, IGrainWithStringKey
    {

    }
}
