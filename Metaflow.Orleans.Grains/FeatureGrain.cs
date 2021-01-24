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
    public class FeatureGrain<TOp, TModel> : Grain, IFeatureGrain<TOp, TModel>
    {
        private readonly EventStoreClient _eventStore;
        private readonly IRequires<TModel> _handler;
        private readonly ILogger<FeatureGrain<TOp, TModel>> _logger;

        public FeatureGrain(EventStoreClient eventStore,
            ILogger<FeatureGrain<TOp, TModel>> logger,
            IRequires<TModel> handler)
        {
            _eventStore = eventStore;
            _logger = logger;
            _handler = handler;
        }

        public Task<FSharpResult<Unit, FeatureFailure>> Call(FeatureCall<TModel> call)
        {
            return FeatureExec.execute(call, _handler, _eventStore, _logger);
        }
    }
}