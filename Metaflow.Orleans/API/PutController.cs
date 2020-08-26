using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Metaflow.Orleans
{

    [DefaultActionConvention]
    public class PutController<TGrain, TResource> : GrainController<TGrain, TResource>
    {
        public PutController(IClusterClient clusterClient) : base(clusterClient)
        {
        }

        [HttpPut(DefaultActionConvention.DefaultRoute)]
        public virtual async Task<IActionResult> Respond(string id, CancellationToken cancellationToken)
        {
            var grain = GetGrain(id);

            using var streamReader = new StreamReader(this.Request.Body, Encoding.UTF8);
            var body = await streamReader.ReadToEndAsync();

            var options = new JsonSerializerOptions().Configure();

            try
            {
                if (this.Request.Headers.Keys.Contains("Metaflow-Batch"))
                {
                    var input = JsonSerializer.Deserialize<List<TResource>>(body, options);

                    var results = new List<Result>();

                    foreach (var i in input)
                        results.Add(await grain.Put(i));

                    var errors = results.Where(r => !r.OK).ToList();

                    TGrain state = await grain.Get();

                    return !errors.Any() ? Ok(state) : (IActionResult)BadRequest(new { state, errors });
                }
                else
                {

                    var input = JsonSerializer.Deserialize<TResource>(body, options);

                    Result result = await grain.Put(input);

                    TGrain state = await grain.Get();

                    return result.OK ? Ok(state) : (IActionResult)BadRequest(result.Error);

                }
            }
            catch (JsonException jex)
            {
                return BadRequest(jex.Message);
            }
        }

    }

}
