using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{

    [Route("[controller]")]
    [MutationControllerConvention]
    [ApiController]
    public class MutationController<TState, TRequest, TArg> : Controller
    where TState : class, new()
    where TRequest : HttpMethodAttribute
    where TArg : class, new()
    {
        private readonly IClusterClient _clusterClient;

        public MutationController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }


        [Route("{id}")]
        public virtual async Task<IActionResult> Respond(string id, TArg input, CancellationToken cancellationToken)
        {
            var grain = _clusterClient.GetGrain<IRestfulGrain<TState>>(id);

            var state = await grain.Get();

            if (Request.Method == "GET") return state == null ? NotFound() : (IActionResult)Ok(state);
            else
            {
                Result<TArg> result = await grain.Handle((MutationRequest)Enum.Parse(typeof(MutationRequest), Request.Method), input);

                return result.OK ? Ok(result.After) : (IActionResult)BadRequest(result.Reason);
            }
        }
    }

}
