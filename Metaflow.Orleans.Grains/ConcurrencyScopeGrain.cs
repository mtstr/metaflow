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

        public Task<FSharpResult<Unit, FeatureFailure>> Execute<TOp, TModel>(
            FeatureCall<TModel> call)
        {
            return Workflows.executeFeature<TOp, TModel>(call, _clusterClient);
        }
    }
}

