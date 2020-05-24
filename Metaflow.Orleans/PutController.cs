using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{
    [DefaultActionConvention]
    public class PutController<TGrain, TResource> : GrainController<TGrain, TResource>

    {

        public PutController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpPut(DefaultActionConvention.DefaultRoute)]
        public virtual async Task<IActionResult> Respond(string id, TResource input, CancellationToken cancellationToken)
        {
            var grain = GetGrain(id);

            Result<TResource> result = await grain.Put(input);

            TGrain state = await grain.Get();

            return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Reason);
        }
    }

}
