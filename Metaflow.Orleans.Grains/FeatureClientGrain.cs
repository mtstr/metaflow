using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Orleans;
using Orleans.Concurrency;

namespace Metaflow.Orleans
{
    [StatelessWorker]
    public class FeatureClientGrain : Grain, IFeatureClientGrain
    {
        private readonly IClusterClient _clusterClient;
        private readonly FSharpMap<Tuple<string, Operation>, Workflow> _workflowMap;

        public FeatureClientGrain(IClusterClient clusterClient, IEnumerable<Workflow> workflows)
        {
            _clusterClient = clusterClient;
            _workflowMap =
                new FSharpMap<Tuple<string, Operation>, Workflow>(
                    workflows.Select(w =>
                        Tuple.Create(
                            Tuple.Create(w.Feature.Model.FullName, w.Feature.Operation), w)));
        }

        public Task<FSharpResult<Unit, WorkflowFailure>> Delete<TModel>(string id, bool awaitState)
        {
            return Features.Delete<TModel>.Execute(_workflowMap, _clusterClient, id, awaitState);
        }
    }
}