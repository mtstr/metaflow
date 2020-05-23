using System;
using System.Threading.Tasks;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain<T> : IRestful<T>, IGrainWithStringKey
    {
        Task<Result<TResource>> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request);
    }
}