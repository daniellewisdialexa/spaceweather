using System;
using Newtonsoft.Json;

namespace SpaceWeatherApi.DataModels
{
    public class SunspotModel
    {
        [JsonProperty("timeTag")]
        public DateTime? TimeTag { get; set; }

        [JsonProperty("obsdate")]
        public string Obsdate { get; set; } = string.Empty;

        [JsonProperty("obstime")]
        public string Obstime { get; set; } = string.Empty;

        [JsonProperty("station")]
        public int Station { get; set; }

        [JsonProperty("observatory")]
        public string Observatory { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("quality")]
        public int Quality { get; set; }

        [JsonProperty("region")]
        public int? Region { get; set; }

        [JsonProperty("latitude")]
        public int Latitude { get; set; }
            
        [JsonProperty("reportLongitude")]
        public int ReportLongitude { get; set; }

        [JsonProperty("longitude")]
        public int Longitude { get; set; }

        [JsonProperty("reportLocation")]
        public string ReportLocation { get; set; } = string.Empty;

        [JsonProperty("location")]
        public string Location { get; set; } = string.Empty;

        [JsonProperty("carlon")]
        public int Carlon { get; set; }

        [JsonProperty("extent")]
        public int? Extent { get; set; }

        [JsonProperty("area")]
        public double Area { get; set; }

        [JsonProperty("numSpot")]
        public double NumSpot { get; set; }

        [JsonProperty("zurich")]
        public int? Zurich { get; set; }

        [JsonProperty("penumbra")]
        public int? Penumbra { get; set; }

        [JsonProperty("compact")]
        public int? Compact { get; set; }

        [JsonProperty("spotClass")]
        public string SpotClass { get; set; } = string.Empty;

        [JsonProperty("magCode")]
        public int MagCode { get; set; }

        [JsonProperty("magClass")]
        public string MagClass { get; set; } = string.Empty;

        [JsonProperty("obsid")]
        public int Obsid { get; set; }

        [JsonProperty("reportStatus")]
        public int ReportStatus { get; set; }

        [JsonProperty("validSpotClass")]
        public int ValidSpotClass { get; set; } 
    }
}