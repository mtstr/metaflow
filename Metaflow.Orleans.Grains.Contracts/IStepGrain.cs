using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using Orleans;

namespace Metaflow.Orleans
{
    public interface IStepGrain<TModel, THandler> : IStepGrain<TModel> where THandler : IStepHandler<TModel>
    {
    }

    public interface IStepGrain<TModel> : IGrainWithStringKey
    {
        Task<StepResult> Call(RequestContext ctx, FSharpResult<FSharpOption<TModel>, FeatureFailure> result);
    }
}