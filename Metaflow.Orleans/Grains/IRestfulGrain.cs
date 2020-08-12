using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.EventSourcing.CustomStorage;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain<T> : IRestful<T>, IGrainWithStringKey, ICustomStorageInterface<GrainState<T>,object>
    {
        Task<Result> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request);

        Task<bool> Exists();
    }
}