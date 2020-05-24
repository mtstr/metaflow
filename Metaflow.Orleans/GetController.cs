using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{
    public class GetController<TState> : GrainController<TState, TState>
    
    {

        public GetController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var state = await GetGrain(id).Get();

            return state == null ? NotFound() : (IActionResult)Ok(state);
        }
    }

}
