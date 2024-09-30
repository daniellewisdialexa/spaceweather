using Microsoft.AspNetCore.Mvc;
using spaceWeatherApi;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/filter")]
    public class FilterController : BaseController
    {
        public FilterController(NasaApiClient nasaApiClient) : base(nasaApiClient) { }

        [HttpGet]
        public async Task<IActionResult> FilterData(string endpoint,[FromQuery] string[] filter, string? startDate = null, string? endDate = null)
        {
            var data = await GetDataAsync(endpoint, startDate, endDate);
            if (data == null)
            {
                return StatusCode(500, "Failed to retrieve data.");
            }

            if (filter != null && filter.Length > 0)
            {
                data = FilterData(data, filter);
            }

            return Ok(data);
        }
    }
}
