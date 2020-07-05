using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{
    [Route("[controller]")]
    [NamingConvention]
    [ApiController]
    public abstract class GrainController<TState, TResource> : ControllerBase
    {
        private readonly IClusterClient _clusterClient;

        protected GrainController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        private Func<TState, Result<TResource>, IActionResult> defaultResponder = (state, result) => result.OK ? new OkObjectResult(state) : (IActionResult)new BadRequestObjectResult(result.Reason);

        protected virtual Task<IActionResult> ProcessRequest<TInput>(
            string id, MutationRequest type, TInput input,
            CancellationToken cancellationToken) => ProcessRequest(id, type, input, defaultResponder, cancellationToken);

        protected virtual async Task<IActionResult> ProcessRequest<TInput>(
            string id, MutationRequest type, TInput input,
            Func<TState, Result<TResource>, IActionResult> responseFunc,
            CancellationToken cancellationToken)
        {
            IRestfulGrain<TState> grain = GetGrain(id);

            Result<TResource> result = await grain.Execute(new CustomRequest<TResource, TInput>(type, input));
            return responseFunc(await grain.Get(), result);
        }

        protected virtual IRestfulGrain<TState> GetGrain(string id)
        {
            return _clusterClient.GetGrain<IRestfulGrain<TState>>(id);
        }
    }

}
