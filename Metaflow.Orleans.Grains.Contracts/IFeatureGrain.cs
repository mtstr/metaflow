using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IFeatureGrain<TOp, TModel, TInput> : IGrainWithStringKey
    {
        Task<FSharpResult<FSharpOption<TModel>, FeatureFailure>> Call(FeatureCall<TInput> call);
    }
}