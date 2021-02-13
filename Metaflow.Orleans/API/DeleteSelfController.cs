using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{
    public class DeleteSelfController<TState> : GrainController<TState, TState>

    {

        public DeleteSelfController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            var grain = GetGrain(id);
            var state = await grain.Get();
            var result = await grain.Delete();

            return Respond(state, result);
        }
    }

}
