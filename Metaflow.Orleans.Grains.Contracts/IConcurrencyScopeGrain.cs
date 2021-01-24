using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IConcurrencyScopeGrain : IGrainWithStringKey
    {
        Task<FSharpResult<Unit, FeatureFailure>> Execute<TOp, TModel>(FeatureCall<TModel> call);
    }
}