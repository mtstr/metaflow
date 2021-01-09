using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _featureClient.Delete<SampleModel>(id, false);

            if (result.FeatureResult.IsOk) return Ok();
            if (result.FeatureResult.IsNotFound) return NotFound();
            if (result.FeatureResult.IsRequestError) return BadRequest();
            if (result.FeatureResult.IsServerError) return StatusCode(500, result);
            
            return NoContent();
        }
    }
}