
using SpaceWeatherApi.DataModels;
namespace SpaceWeatherApi.Utils
{
    public class SolarReportingUtils
    {

        /// <summary>
        /// Generate a solar region report from the given solar region data and sunspot data.
        /// </summary>
        /// <param name="allSolarRegionData"></param>
        /// <param name="allSunspotData"></param>
        /// <returns>List of solar region data ordered by the date observed</returns>
        public List<SolarRegionReportItem> GenerateSolarRegionReport(List<SolarRegionModel> allSolarRegionData, List<SunspotModel> allSunspotData)
        {
            return [.. allSolarRegionData
                .Select(sr => CreateSolarRegionReportItem(sr, allSunspotData))
                .OrderByDescending(r => r.ObservedDate)];
        }

        /// <summary>
        /// Create a solar region report item from the given solar region and sunspot data.
        /// </summary>
        /// <param name="solarRegion"></param>
        /// <param name="allSunspotData"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the matching sunspots for the given region.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="allSunspotData"></param>
        /// <returns>List of matching sunspots</returns>
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
