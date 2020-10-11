using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{

    [DefaultActionConvention]
    public class PatchController<TGrain, TInput> : GrainController<TGrain, TGrain>
    {
        public PatchController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpPatch(DefaultActionConvention.DefaultRoute)]
        public virtual async Task<IActionResult> Respond(string id, TInput input, CancellationToken cancellationToken)
        {
            IRestfulGrain<TGrain> grain = GetGrain(id);

            var result = await grain.Patch(input);

            var state = await grain.Get();

            return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Error);
        }
    }

}
