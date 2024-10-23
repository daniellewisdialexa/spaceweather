using SpaceWeatherApi.DataModels;
namespace SpaceWeatherApi.Services.Interfaces
{
    public interface ISpaceWeatherService
    {
        string GenerateSpaceWeatherReport(
            List<SolarRegionModel> allSolarRegionData,
            List<SunspotModel> allSunspotData,
            List<FlareEvent> allFlareEvents,
            List<CMEEvent> allCMEEvents,
            FluxModel fluxData);
    }
}
