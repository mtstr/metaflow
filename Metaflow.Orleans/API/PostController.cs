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
        public virtual async Task<IActionResult> Post(string id, TResource input, CancellationToken cancellationToken)
        {
            IRestfulGrain<TGrain> grain = GetGrain(id);

            if (input is TGrain && await grain.Exists()) return Conflict();

            var result = await grain.Post(input);

            var state = await grain.Get();

            return Respond(state,result);
        }
    }
}
