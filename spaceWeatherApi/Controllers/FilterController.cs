using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.Utils;
using SpaceWeatherApi.Utils.Extentions;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/filter")]
    public class FilterController(IApiClient apiClient) : BaseController(apiClient)
    {
        /// <summary>
        /// filter data
        /// </summary>
        /// <param name="endpoint"></param> (e.g. "FLR", "CME")
        /// <param name="filter"></param> - The json property 
        /// <param name="startDate"></param>1
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FilterData(string endpoint,[FromQuery] string[] filter, string? startDate = null, string? endDate = null)
        {
            var data = await ApiClient.GetDataAsync(endpoint, startDate, endDate);
            if (data == null)
            {
                return StatusCode(500, "Failed to retrieve data.");
            }

            if (filter != null && filter.Length > 0)
            {
                data = ListExtensions.FilterData(data, filter);
            }
            return Ok(data);
        }
    }
}
