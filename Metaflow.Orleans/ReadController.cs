using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{

    [Route("[controller]")]
    [ReadControllerConvention]
    [ApiController]
    public class ReadController<TState> : Controller
    where TState : class, new()
    {
        private readonly IClusterClient _clusterClient;

        public ReadController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            var grain = _clusterClient.GetGrain<IRestfulGrain<TState>>(id);

            var state = await grain.Get();

            return state == null ? NotFound() : (IActionResult)Ok(state);
        }
    }

}
