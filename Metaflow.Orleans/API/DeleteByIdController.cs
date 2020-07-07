using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{
    [DefaultActionConvention]
    public class DeleteByIdController<TState, TResource> : GrainController<TState, TResource>
    {
        public DeleteByIdController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpDelete("{id}/{itemId}")]
        public virtual Task<IActionResult> Delete(string id, string itemId, CancellationToken cancellationToken)
        {
            return ProcessRequest(id, MutationRequest.DELETE, itemId, cancellationToken);
        }
    }

}
