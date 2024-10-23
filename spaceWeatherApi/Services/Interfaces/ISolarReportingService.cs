using SpaceWeatherApi.DataModels;

namespace SpaceWeatherApi.Services.Interfaces
{
    public interface ISolarReportingService
    {
        List<SolarRegionReportItem> GenerateSolarRegionReport(List<SolarRegionModel> allSolarRegionData, List<SunspotModel> allSunspotData);
    }
}