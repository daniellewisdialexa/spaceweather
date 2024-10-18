using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/filter")]
    public class FilterController(ApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        /// <summary>
        /// filter data
        /// </summary>
        /// <param name="endpoint"></param> (e.g. "FLR", "CME")
        /// <param name="filter"></param> - The json property 
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FilterData(string endpoint,[FromQuery] string[] filter, string? startDate = null, string? endDate = null)
        {
            var data = await _ApiClient.GetDataAsync(endpoint, startDate, endDate);
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
