using Newtonsoft.Json;

namespace SpaceWeatherApi.DataModels
{
    public class Instrument
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }

}
