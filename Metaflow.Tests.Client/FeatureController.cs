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

            if (result.IsOk) return Ok();
            if (result.IsNotFound) return NotFound();
            if (result.IsRequestError) return BadRequest();
            if (result.IsServerError) return StatusCode(500, result);
            
            return NoContent();
        }
    }
}