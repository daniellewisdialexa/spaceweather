namespace SpaceWeatherApi.DataModels
{
    public class InterestingEvent
    {
        public required FlareEvent Flare { get; set; }
        public double CMESpeed { get; set; } = double.NaN;
        public double SurpriseFactor { get; set; }
        public string Reason { get; set; } = string.Empty;
        public double Confidence { get; internal set; }
        public CMEEvent? CME { get; internal set; }
    }
}
