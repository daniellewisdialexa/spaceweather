namespace SpaceWeatherApi.DataModels
{
    using System;
    using Newtonsoft.Json;

    //TODO: Updated all data models to use nullable reference types
    public class SolarRegionModel
    {
        [JsonProperty("observed_date")]
        public DateTime? ObservedDate { get; set; }

        [JsonProperty("region")]
        public int? Region { get; set; }

        [JsonProperty("latitude")]
        public float? Latitude { get; set; }

        [JsonProperty("longitude")]
        public float? Longitude { get; set; }

        [JsonProperty("location")]
        public string? Location { get; set; } 

        [JsonProperty("carrington_longitude")]
        public float? CarringtonLongitude { get; set; }

        [JsonProperty("old_carrington_longitude")]
        public float? OldCarringtonLongitude { get; set; }

        [JsonProperty("area")]
        public int? Area { get; set; }

        [JsonProperty("spot_class")]
        public string? SpotClass { get; set; }

        [JsonProperty("extent")]
        public int? Extent { get; set; }

        [JsonProperty("number_spots")]
        public double? NumberSpots { get; set; }

        [JsonProperty("mag_class")]
        public string? MagClass { get; set; }

        [JsonProperty("mag_string")]
        public string? MagString { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; } 

        [JsonProperty("c_xray_events")]
        public int? CXrayEvents { get; set; }

        [JsonProperty("m_xray_events")]
        public int? MXrayEvents { get; set; }

        [JsonProperty("x_xray_events")]
        public int? XXrayEvents { get; set; }

        [JsonProperty("proton_events")]
        public int? ProtonEvents { get; set; }

        [JsonProperty("s_flares")]
        public int? SFlares { get; set; }

        [JsonProperty("impulse_flares_1")]
        public int? ImpulseFlares1 { get; set; }

        [JsonProperty("impulse_flares_2")]
        public int? ImpulseFlares2 { get; set; }

        [JsonProperty("impulse_flares_3")]
        public int? ImpulseFlares3 { get; set; }

        [JsonProperty("impulse_flares_4")]
        public int? ImpulseFlares4 { get; set; }

        [JsonProperty("protons")]
        public int? Protons { get; set; }

        [JsonProperty("c_flare_probability")]
        public int? CFlareProbability { get; set; }

        [JsonProperty("m_flare_probability")]
        public int? MFlareProbability { get; set; }

        [JsonProperty("x_flare_probability")]
        public int? XFlareProbability { get; set; }

        [JsonProperty("proton_probability")]
        public int? ProtonProbability { get; set; }

        [JsonProperty("first_date")]
        public DateTime? FirstDate { get; set; }
    }
}