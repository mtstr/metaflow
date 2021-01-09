using System.Threading.Tasks;
using Microsoft.FSharp.Control;

namespace Metaflow.Tests.Client
{
    public class FirstStepHandler : IStepHandler<SampleModel>
    {
        public Task<StepResult> Call(RequestContext ctx,
            FeatureResult<SampleModel> result)
        {
            throw new System.NotImplementedException();
        }
    }

    public class SampleModel
    {
    }
}