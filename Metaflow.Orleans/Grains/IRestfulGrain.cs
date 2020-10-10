using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.EventSourcing.CustomStorage;

namespace Metaflow.Orleans
{
    public interface IRestfulGrain<T> : IRestful<T>, IGrainWithStringKey
    {
        Task<Result> Execute<TResource, TInput>(CustomRequest<TResource, TInput> request);

        Task<bool> Exists();

        Task<int> GetVersion();
    }
}