using Microsoft.AspNetCore.Mvc;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/group")]
    public class GroupController(ApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        /// <summary>
        /// Get the data grouped by the specified property
        /// </summary>
        /// <param name="endpoint"></param> (FLR, CME)
        /// <param name="property"></param> The json property to group by
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet]
       public async Task<IActionResult> GetGroupedData(string endpoint, [FromQuery] string property, string? startDate = null, string? endDate = null)
        {
            var data = await _ApiClient.GetDataAsync(endpoint, startDate, endDate);
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
