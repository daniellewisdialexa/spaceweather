using Microsoft.AspNetCore.Mvc;
using spaceWeatherApi;

namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}")]
    public class StartingPointController(NasaApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        [HttpGet]
        public async Task<IActionResult> GetAllData(string endpoint, [FromQuery] string? startDate = null, string? endDate = null)
        {
            var data = await _nasaApiClient.GetDataAsync(endpoint.ToUpper(), startDate, endDate);

            if (data == null)
            {
                return NotFound("No data found for the specified endpoint.");
            }

            return Ok(data);
        }

        [HttpGet("count")]
        public async Task<IActionResult> CountProperties(string endpoint, string property, string? startDate = null, string? endDate = null)
        {
            var data = await _nasaApiClient.GetDataAsync(endpoint.ToUpper(), startDate, endDate);

            if (data == null)
            {
                return NotFound("No data found for the specified endpoint.");
            }

            var count = CountOfProperty(data, property);
            return Ok(new { PropertyCount = count });
        }
    }
}
