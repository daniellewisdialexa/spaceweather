using Microsoft.AspNetCore.Mvc;
using spaceWeatherApi;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/{endpoint}/order")]
    public class OrderController : BaseController
    {
        public OrderController(NasaApiClient nasaApiClient) : base(nasaApiClient) { }

        [HttpGet]
        public async Task<IActionResult> OrderFlareData(string endpoint,[FromQuery] Order order, string property, string? startDate = null, string? endDate = null)
        {
            var data = await GetDataAsync(endpoint, startDate, endDate);
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
