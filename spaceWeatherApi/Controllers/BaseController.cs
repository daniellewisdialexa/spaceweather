using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.Services.Interfaces;


namespace SpaceWeatherApi.Controllers
{
    public abstract class BaseController(
     IApiClient apiClient,
     ISolarReportingService? solarReportingService = null,
     ISpaceWeatherService? spaceWeatherService = null
 ) : ControllerBase
    {
        protected readonly IApiClient ApiClient = apiClient;
        protected readonly ISolarReportingService? SolarReportingService = solarReportingService;
        protected readonly ISpaceWeatherService? SpaceWeatherService = spaceWeatherService;

    }
}
