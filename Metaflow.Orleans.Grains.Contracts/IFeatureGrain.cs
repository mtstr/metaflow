using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IFeatureGrain<TOp, TModel> : IGrainWithStringKey
    {
        Task<FSharpResult<Unit, FeatureFailure>> Call(FeatureCall<TModel> call);
    }
}