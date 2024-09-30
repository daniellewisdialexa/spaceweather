using Microsoft.AspNetCore.Mvc;
using spaceWeatherApi;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/group")]
    public class GroupController(NasaApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        [HttpGet]
        public async Task<IActionResult> GetGroupedData(string endpoint, [FromQuery] string property, string? startDate = null, string? endDate = null)
        {
            var data = await GetDataAsync (endpoint, startDate, endDate);
            if (data == null)
            {
                return StatusCode(500, "Failed to retrieve data.");
            }
            // Group the data by the specified property
            var groupedData = GroupByProperty(data, property);

            return Ok(groupedData);
        }


    }
}
