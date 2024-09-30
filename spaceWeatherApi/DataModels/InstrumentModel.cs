using Newtonsoft.Json;

namespace spaceWeatherApi.DataModels
{
    public class Instrument
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }

}
