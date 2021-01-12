using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{

    [DefaultActionConvention]
    public class DeleteController<TGrain, TResource> : GrainController<TGrain, TResource>

    {
        public DeleteController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpDelete(DefaultActionConvention.DefaultRoute)]
        public virtual async Task<IActionResult> Delete(string id, TResource resource, CancellationToken cancellationToken)
        {
            IRestfulGrain<TGrain> grain = GetGrain(id);

            var result = await grain.Delete<TResource>(resource);

            var state = await grain.Get();

            return Response(state,result);
        }
    }

}
