using Newtonsoft.Json;

namespace SpaceWeatherApi.DataModels
{

    public class CMEAnalysis
    {
        [JsonProperty("isMostAccurate")]
        public bool IsMostAccurate { get; set; }

        [JsonProperty("time21_5")]
        public DateTime Time21_5 { get; set; }

        [JsonProperty("latitude")]
        public float? Latitude { get; set; }

        [JsonProperty("longitude")]
        public float? Longitude { get; set; }

        [JsonProperty("halfAngle")]
        public float? HalfAngle { get; set; }

        [JsonProperty("speed")]
        public float? Speed { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("featureCode")]
        public string FeatureCode { get; set; } = string.Empty;

        [JsonProperty("imageType")]
        public string ImageType { get; set; } = string.Empty;

        [JsonProperty("measurementTechnique")]
        public string MeasurementTechnique { get; set; } = string.Empty;

        [JsonProperty("note")]
        public string Note { get; set; } = string.Empty;

        [JsonProperty("levelOfData")]
        public int? LevelOfData { get; set; }

        [JsonProperty("tilt")]
        public float? Tilt { get; set; }

        [JsonProperty("minorHalfWidth")]
        public float? MinorHalfWidth { get; set; }

        [JsonProperty("speedMeasuredAtHeight")]
        public double? SpeedMeasuredAtHeight { get; set; }

        [JsonProperty("submissionTime")]
        public DateTime? SubmissionTime { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;

        [JsonProperty("enlilList")]
        public object? EnlilList { get; set; }
    }
}
