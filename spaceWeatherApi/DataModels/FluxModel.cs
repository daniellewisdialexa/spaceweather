namespace SpaceWeatherApi.DataModels
{
    using System;
    using Newtonsoft.Json;


    public class FluxModel
    {
        [JsonProperty("timeStamp")]
        public string? TimeStamp { get; set; }

        [JsonProperty("flux")]
        public string? Flux { get; set; }
    }

}
