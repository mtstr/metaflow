using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public class ConcurrencyScopeGrain : Grain, IConcurrencyScopeGrain
    {
        private readonly IClusterClient _clusterClient;

        public ConcurrencyScopeGrain(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public Task<FSharpResult<FSharpOption<TModel>, FeatureFailure>> Execute<TOp, TModel, TInput>(
            FeatureCall<TInput> call)
        {
            return Workflows.executeFeature<TOp, TModel, TInput>(call, _clusterClient);
        }
    }
}