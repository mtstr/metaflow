using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{
    [DefaultActionConvention]
    public class PostController<TGrain, TResource> : GrainController<TGrain, TResource>
    {
        public PostController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpPost(DefaultActionConvention.DefaultRoute)]
        public virtual async Task<IActionResult> Respond(string id, TResource input, CancellationToken cancellationToken)
        {
            var grain = GetGrain(id);

            Result<TResource> result = await grain.Post(input);

            TGrain state = await grain.Get();

            return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Reason);
        }
    }

}
