namespace SpaceWeatherApi.DataModels
{
    public class SunspotData
    {
        public DateTime TimeTag { get; set; }
        public string Obsdate { get; set; } = string.Empty;
        public string Obstime { get; set; } = string.Empty;
        public int Station { get; set; }
        public string Observatory { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Quality { get; set; }
        public int? Region { get; set; }
        public int? Latitude { get; set; }
        public int? ReportLongitude { get; set; }
        public int? Longitude { get; set; }
        public string ReportLocation { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int? Carlon { get; set; }
        public int? Extent { get; set; }
        public double Area { get; set; }
        public double NumSpot { get; set; }
        public int? Zurich { get; set; }
        public int? Penumbra { get; set; }
        public int? Compact { get; set; }
        public string SpotClass { get; set; } = string.Empty;
        public int? MagCode { get; set; }
        public string MagClass { get; set; } = string.Empty;
        public int? Obsid { get; set; }
        public int? ReportStatus { get; set; }
        public int? ValidSpotClass { get; set; }
    }
}
