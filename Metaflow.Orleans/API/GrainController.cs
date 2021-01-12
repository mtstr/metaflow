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
    [ServiceFilter(typeof(GrainActionFilterAttribute))]
    public abstract class GrainController<TState, TResource> : ControllerBase
    {
        private readonly IClusterClient _clusterClient;

        protected GrainController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        protected Func<TState, Result, IActionResult> Response = (state, result) => result.OK ? new OkObjectResult(state) : (IActionResult)new BadRequestObjectResult(new { result.Error, result.Events });

        protected virtual Task<IActionResult> ProcessRequest<TInput>(
            string id, MutationRequest type, TInput input,
            CancellationToken cancellationToken) => ProcessRequest(id, type, input, Response, cancellationToken);

        protected virtual async Task<IActionResult> ProcessRequest<TInput>(
            string id, MutationRequest type, TInput input,
            Func<TState, Result, IActionResult> responseFunc,
            CancellationToken cancellationToken)
        {
            IRestfulGrain<TState> grain = GetGrain(id);

            var result = await grain.Execute(new CustomRequest<TResource, TInput>(type, input));
            return responseFunc(await grain.Get(), result);
        }

        protected virtual IRestfulGrain<TState> GetGrain(string id)
        {
            return _clusterClient.GetGrain<IRestfulGrain<TState>>(id);
        }
    }

}
