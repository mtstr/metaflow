using System.Threading.Tasks;
using Metaflow.Orleans;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;

namespace Metaflow.Tests.Client
{
    [ApiController]
    [Route("")]
    public class FeatureController : ControllerBase
    {
        private readonly FeatureClient _featureClient;


        public FeatureController(FeatureClient featureClient)
        {
            _featureClient = featureClient;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            FSharpResult<Unit, WorkflowFailure> result = await _featureClient.Delete<SampleModel>(id, false);

            if (result.IsOk) return Ok();
            if (result.ErrorValue.IsFeatureFailure)
            {
                var featureFailure = result.ErrorValue.FeatureFailureValue;
                if (featureFailure.IsNotFound) return NotFound();

                if (featureFailure.IsRequestError) return BadRequest();
                if (featureFailure.IsServerError) return StatusCode(500, featureFailure);
            }

            return NoContent();
        }
    }
}