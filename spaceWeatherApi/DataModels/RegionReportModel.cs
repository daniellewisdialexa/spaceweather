namespace SpaceWeatherApi.DataModels
{
    public class RegionReportModel
    {
        public int? Region { get; set; }
        public double TotalSunspots { get; set; }
        public double RecentSunspots { get; set; }
        public List<FlareEvent>? SignificantFlares { get; set; }
        public List<FlareEvent>? RecentSignificantFlares { get; set; }
        public int CMECount { get; set; }
        public int RecentCMECount { get; set; }
    }
}
