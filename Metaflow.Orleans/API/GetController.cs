using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FSharp.Data.JsonSchema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orleans;

namespace Metaflow.Orleans
{
    public class GetController<TState> : GrainController<TState, TState>
    {
        private readonly IQuerySync<TState> _querySync;
        public GetController(IClusterClient clusterClient, IQuerySync<TState> querySync = null) : base(clusterClient)
        {
            _querySync = querySync;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
        {
            IRestfulGrain<TState> grain = GetGrain(id);

            bool v = await grain.Exists();

            return v ? base.Ok(await grain.Get()) : (IActionResult)base.NotFound();
        }

        [HttpGet("schema")]
        public virtual async Task<IActionResult> GetSchemaSample(CancellationToken cancellationToken)
        {
            return Ok(new Fixture().Create<TState>());
        }

        [HttpPost("{id}/queryable")]
        public virtual async Task<IActionResult> Post(string id, CancellationToken cancellationToken)
        {
            IRestfulGrain<TState> grain = GetGrain(id);

            bool v = await grain.Exists();

            if (v && _querySync != null) await _querySync.UpdateQueryStore(id, await grain.Get());

            return NoContent();
        }
    }

}
