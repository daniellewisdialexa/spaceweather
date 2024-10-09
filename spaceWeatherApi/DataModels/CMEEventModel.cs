namespace SpaceWeatherApi.DataModels
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class CMEEvent
    {
        [JsonProperty("activityID")]
        public string ActivityID { get; set; } = string.Empty;

        [JsonProperty("catalog")]
        public string Catalog { get; set; } = string.Empty;

        [JsonProperty("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("instruments")]
        public List<Instrument> Instruments { get; set; } = [];

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

        [JsonProperty("cmeAnalyses")]
        public List<CMEAnalysis> CMEAnalyses { get; set; } = [];

        [JsonProperty("linkedEvents")]
        public object? LinkedEvents { get; set; }
    }
}
