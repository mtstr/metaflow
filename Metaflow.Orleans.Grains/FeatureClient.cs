using System;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public class FeatureClient
    {
        private readonly IClusterClient _clusterClient;

        public FeatureClient(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public Task<FSharpResult<Unit, WorkflowFailure>> Delete<TModel>(string aggregateRootId, bool awaitState)
        {
            var grain =
                _clusterClient.GetGrain<IFeatureClientGrain>(Guid.NewGuid().ToString());

            return grain.Delete<TModel>(aggregateRootId, awaitState);
        }
    }
}