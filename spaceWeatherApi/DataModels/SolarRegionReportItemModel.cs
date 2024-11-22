using SpaceWeatherApi.Utils;

namespace SpaceWeatherApi.DataModels
{
    public class SolarRegionReportItem
    {
        public DateTime? ObservedDate { get; set; }
        public int? Region { get; set; }
        public double? NumberSpots { get; set; }
        public string? SpotClass { get; set; }
        public DateTime? FirstDate { get; set; }
        public List<SunspotReportItem>? MatchingSunspots { get; set; }
    }
}
