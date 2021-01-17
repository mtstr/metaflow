using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IConcurrencyScopeGrain : IGrainWithStringKey
    {
        Task<FSharpResult<FSharpOption<TModel>, FeatureFailure>> Execute<TOp, TModel, TInput>(FeatureCall<TInput> call);
    }
}