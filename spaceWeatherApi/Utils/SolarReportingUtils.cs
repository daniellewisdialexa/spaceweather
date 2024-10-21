
using SpaceWeatherApi.DataModels;
namespace SpaceWeatherApi.Utils
{

    public class SolarReportingUtils
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

    public class SolarRegionReportItem
    {
        public DateTime? ObservedDate { get; set; }
        public int? Region { get; set; }
        public double? NumberSpots { get; set; }
        public string? SpotClass { get; set; }
        public DateTime? FirstDate { get; set; }
        public List<SunspotReportItem>? MatchingSunspots { get; set; }
    }

    public class SunspotReportItem
    {
        public string? Obsdate { get; set; }
        public double? NumSpot { get; set; }
        public string? SpotClass { get; set; }
    }
}
