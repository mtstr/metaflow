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
    {
        private readonly IClusterClient _clusterClient;

        public MutationController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }


        [Route("{id}")]
        public virtual async Task<IActionResult> Respond(string id, TArg input, CancellationToken cancellationToken)
        {
            IRestfulGrain<TState> grain = _clusterClient.GetGrain<IRestfulGrain<TState>>(id);

            Result<TArg> result = (MutationRequest)Enum.Parse(typeof(MutationRequest), Request.Method) switch
            {
                MutationRequest.PUT => await grain.Put(input),
                MutationRequest.POST => await grain.Post(input),
                _ => Result<TArg>.Nok("Invalid action for request")
            };

            TState state = await grain.Get();

            return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Reason);
        }
    }

}
