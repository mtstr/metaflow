using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public class StepGrain<TModel, THandler> : Grain, IStepGrain<TModel, THandler> where THandler : IStepHandler<TModel>
    {
        private readonly THandler _handler;

        public StepGrain(THandler handler)
        {
            _handler = handler;
        }

        public Task<StepResult> Call(RequestContext ctx, FSharpResult<FSharpOption<TModel>, FeatureFailure> result)
        {
            return _handler.Call(ctx, result);
        }
    }
}