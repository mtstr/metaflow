using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{
    public class DeleteByIdController<TState, TResource, TId> : GrainController<TState, TResource>
    {
        public DeleteByIdController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpDelete("{id}/items/{itemId}")]
        public virtual Task<IActionResult> Delete(string id, TId itemId, CancellationToken cancellationToken)
        {
            return ProcessRequest(id, MutationRequest.DELETE, itemId, cancellationToken);
        }
    }

}
