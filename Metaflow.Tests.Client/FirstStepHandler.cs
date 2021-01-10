using System.Threading.Tasks;

namespace Metaflow.Tests.Client
{
    public class FirstStepHandler : IStepHandler<SampleModel>
    {
        public async Task<StepResult> Call(RequestContext ctx,
            FeatureResult<SampleModel> result)
        {
            return StepResult.Done;
        }
    }
}