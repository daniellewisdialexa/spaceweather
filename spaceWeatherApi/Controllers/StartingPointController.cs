using Microsoft.AspNetCore.Mvc;

namespace SpaceWeatherApi.Controllers
{

    [ApiController]
    [Route("api/{endpoint}")]
    public class StartingPointController(ApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        [HttpGet]
        public async Task<IActionResult> GetAllData(string endpoint, [FromQuery] string? startDate = null, string? endDate = null)
        {
            var data = await _ApiClient.GetDataAsync(endpoint.ToUpper(), startDate, endDate);

            if (data == null)
            {
                return NotFound("No data found for the specified endpoint.");
            }

            return Ok(data);
        }

        /// <summary>
        /// Get a count of the specified property in the data
        /// </summary>
        /// <param name="endpoint"></param> (FLR, CME)
        /// <param name="property"></param> JSON property 
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("count")]
        public async Task<IActionResult> CountProperties(string endpoint, string property, string? startDate = null, string? endDate = null)
        {
            var data = await _ApiClient.GetDataAsync(endpoint.ToUpper(), startDate, endDate);

            if (data == null)
            {
                return NotFound("No data found for the specified endpoint.");
            }

            var count = CountOfProperty(data, property);
            return Ok(new { PropertyCount = count });
        }
    }
}
