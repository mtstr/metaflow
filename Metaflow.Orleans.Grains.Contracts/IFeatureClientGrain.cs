using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IFeatureClientGrain : IGrainWithStringKey
    {
        Task<FSharpResult<Unit, WorkflowFailure>> Delete<TModel>(string id, bool awaitState);
    }
}