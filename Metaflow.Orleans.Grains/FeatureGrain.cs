using System.Threading.Tasks;
using EventStore.Client;
using Metaflow;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Orleans;
using Orleans.CodeGeneration;

[assembly: KnownAssembly(typeof(FeatureCall<>))] 

namespace Metaflow.Orleans
{
    public class FeatureGrain<TOp, TModel, TInput> : Grain, IFeatureGrain<TOp, TModel, TInput>
    {
        private readonly EventStoreClient _eventStore;
        private readonly FeatureHandler<TOp, TModel, TInput> _handler;
        private readonly ILogger<FeatureGrain<TOp, TModel, TInput>> _logger;

        public FeatureGrain(EventStoreClient eventStore,
            ILogger<FeatureGrain<TOp, TModel, TInput>> logger,
            FeatureHandler<TOp, TModel, TInput> handler)
        {
            _eventStore = eventStore;
            _logger = logger;
            _handler = handler;
        }

        public Task<FSharpResult<FSharpOption<TModel>, FeatureFailure>> Call(FeatureCall<TInput> call)
        {
            return FeatureExec.execute(call, _handler, _eventStore, _logger);
        }
    }
}