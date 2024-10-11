using Microsoft.AspNetCore.Mvc;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/order")]
    public class OrderController(ApiClient nasaApiClient) : BaseController(nasaApiClient)
    {
        /// <summary>
        /// Order the data by the given property
        /// </summary>
        /// <param name="endpoint"></param> (FLR or CME)
        /// <param name="order"></param> (Asc or Desc)
        /// <param name="property"></param> The property to order by
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> OrderFlareData(string endpoint,[FromQuery] Order order, string property, string? startDate = null, string? endDate = null)
        {
            var data = await _ApiClient.GetDataAsync(endpoint, startDate, endDate);
            if (data == null)
            {
                return StatusCode(500, "Failed to retrieve data.");
            }

            if (!string.IsNullOrEmpty(property))
            {
                data = OrderBy(data, order, property);
            }

            return Ok(data);
        }
    }
}
