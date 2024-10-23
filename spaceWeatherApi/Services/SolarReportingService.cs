using SpaceWeatherApi.DataModels;
using SpaceWeatherApi.Services.Interfaces;


namespace SpaceWeatherApi.Services
{
    public class SolarReportingService : ISolarReportingService
    {
        public List<SolarRegionReportItem> GenerateSolarRegionReport(List<SolarRegionModel> allSolarRegionData, List<SunspotModel> allSunspotData)
        {
            return allSolarRegionData
                .Select(sr => CreateSolarRegionReportItem(sr, allSunspotData))
                .OrderByDescending(r => r.ObservedDate)
                .ToList();
        }

        private static SolarRegionReportItem CreateSolarRegionReportItem(SolarRegionModel solarRegion, List<SunspotModel> allSunspotData)
        {
            var matchingSunspots = GetMatchingSunspots(solarRegion.Region, allSunspotData);
            return new SolarRegionReportItem
            {
                ObservedDate = solarRegion.ObservedDate,
                Region = solarRegion.Region,
                NumberSpots = matchingSunspots.Sum(ss => ss.NumSpot),
                SpotClass = solarRegion.SpotClass,
                FirstDate = solarRegion.FirstDate,
                MatchingSunspots = matchingSunspots
            };
        }

        private static List<SunspotReportItem> GetMatchingSunspots(int? region, List<SunspotModel> allSunspotData)
        {
            return allSunspotData
                .Where(ss => ss.Region == region)
                .Select(ss => new SunspotReportItem
                {
                    Obsdate = ss.Obsdate,
                    NumSpot = ss.NumSpot,
                    SpotClass = ss.SpotClass
                })
                .ToList();
        }
    }
}