using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{

    [Route("[controller]")]
    [DeleteSelfControllerConvention]
    [ApiController]
    public class DeleteSelfController<TState> : Controller
    where TState : class, new()
    {
        private readonly IClusterClient _clusterClient;

        public DeleteSelfController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var grain = _clusterClient.GetGrain<IRestfulGrain<TState>>(id);

            var result = await grain.Delete();

            return result.OK ? NoContent() : (IActionResult)BadRequest(result.Reason);
        }
    }

}
