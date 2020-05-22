using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{

    [Route("[controller]")]
    [DeleteControllerConvention]
    [ApiController]
    public class DeleteController<TState, TResource> : Controller
    where TState : class, new()
    {
        private readonly IClusterClient _clusterClient;

        public DeleteController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(string id, TResource resource, CancellationToken cancellationToken)
        {
            var grain = _clusterClient.GetGrain<IRestfulGrain<TState>>(id);

            var result = await grain.Delete<TResource>(resource);

            TState state = await grain.Get();

            return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Reason);
        }
    }

}
