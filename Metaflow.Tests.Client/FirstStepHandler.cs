using System.Threading.Tasks;
using Microsoft.FSharp.Core;

namespace Metaflow.Tests.Client
{
    public class FirstStepHandler : IStepHandler<SampleModel>
    {
        public Task<StepResult> Call(RequestContext ctx,
            FSharpResult<Unit, FeatureFailure> result)
        {
            return Task.FromResult(StepResult.Done);
        }
    }
}