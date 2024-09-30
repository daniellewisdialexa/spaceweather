using Newtonsoft.Json;

namespace spaceWeatherApi.DataModels
{
    public class FlareEvent
    {
        [JsonProperty("flrID")]
        public string FlrID { get; set; } = string.Empty;

        [JsonProperty("instruments")]
        public List<Instrument> Instruments { get; set; } = [];

        [JsonProperty("beginTime")]
        public DateTime? BeginTime { get; set; } 

        [JsonProperty("peakTime")]
        public DateTime? PeakTime { get; set; } 

        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; } 

        [JsonProperty("classType")]
        public string ClassType { get; set; } = string.Empty;

        [JsonProperty("sourceLocation")]
        public string SourceLocation { get; set; } = string.Empty;

        [JsonProperty("activeRegionNum")]
        public int? ActiveRegionNum { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; } = string.Empty;

        [JsonProperty("submissionTime")]
        public DateTime? SubmissionTime { get; set; }

        [JsonProperty("versionId")]
        public int VersionId { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;

        [JsonProperty("linkedEvents")]
        public object? LinkedEvents { get; set; }
    }

}
