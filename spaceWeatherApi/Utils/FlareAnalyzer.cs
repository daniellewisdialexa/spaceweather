using spaceWeatherApi.DataModels;

namespace spaceWeatherApi.Utils
{
    public class FlareAnalyzer(NasaApiClient nasaApiClient)
    {
        protected readonly NasaApiClient _nasaApiClient = nasaApiClient;
        private const int CME_ASSOCIATION_WINDOW_HOURS = 6; //Move to appsettings.json?
 
        private static List<CMEEvent> GetAssociatedCMEs(FlareEvent flare, List<CMEEvent> allCMEs)
        {
            return allCMEs.Where(cme =>
            {
                if (cme.StartTime.HasValue && flare.BeginTime.HasValue)
                {
                    TimeSpan timeDifference = cme.StartTime.Value - flare.BeginTime.Value;
                    return Math.Abs(timeDifference.TotalHours) <= CME_ASSOCIATION_WINDOW_HOURS;
                }
                return false; 
            }).ToList();
        }


        private static double CalculateConfidenceLevel(FlareEvent flare, CMEEvent? cme)
        {
            double confidence = 1.0;

            if (cme == null)
            {
                confidence *= 0.5;
            }

            if (flare.BeginTime == null || string.IsNullOrEmpty(flare.ClassType))
            {
                confidence *= 0.7;
            }

            if (cme != null && cme.CMEAnalyses != null && cme.CMEAnalyses.Count != 0)
            {
                var latestAnalysis = cme.CMEAnalyses.OrderByDescending(a => a.Time21_5).FirstOrDefault();
                if (latestAnalysis == null || latestAnalysis.Speed == null || latestAnalysis.Speed.Value == 0)
                {
                    confidence *= 0.8;
                }
            }
            else
            {
                confidence *= 0.8;
            }

            return confidence;
        }



        private static double CalculateCMESpeed(CMEEvent cme)
        {
            if (cme.CMEAnalyses != null && cme.CMEAnalyses.Count != 0)
            {
              
                var mostAccurateAnalysis = cme.CMEAnalyses.FirstOrDefault(a => a.IsMostAccurate);
                if (mostAccurateAnalysis != null && mostAccurateAnalysis.Speed.HasValue)
                {
                    return mostAccurateAnalysis.Speed.Value;
                }

                // If no most accurate analysis, get the latest analysis
                var latestAnalysis = cme.CMEAnalyses.OrderByDescending(a => a.Time21_5).FirstOrDefault();
                if (latestAnalysis != null && latestAnalysis.Speed.HasValue)
                {
                    return latestAnalysis.Speed.Value;
                }
            }

            return 0; 
        }



        private readonly Dictionary<char, (double Min, double Max)> expectedSpeedRanges = new()
            {
                {'C', (300.0, 800.0)},//C-class: 300-800 km/s
                {'M', (500.0, 1200.0)},//M-class: 500-1200 km/s
                {'X', (800.0, 2000.0)}//X-class: 800-2000 km/s
            };

        private async Task<List<SunspotData>> GetAllSunspotData()
        { 
            return await _nasaApiClient.GetNOAAData<SunspotData>("sunspot");
        }

        private static SunspotData? FindRelevantSunspotData(FlareEvent flare, List<SunspotData> allSunspotData)
        {
            if (flare == null || allSunspotData == null || flare.ActiveRegionNum == null)
            {
                Console.WriteLine("Flare, allSunspotData, or ActiveRegionNum is null");
                return null;
            }

            string flareRegion = flare.ActiveRegionNum.ToString() ?? "";

            var relevantSunspots = allSunspotData
             .Where(s => s.Region != null &&
                 !string.IsNullOrEmpty(s.Region.ToString()) &&
                 flareRegion.Contains(s.Region.ToString() ?? ""))
              .ToList();

        
            var closestSunspot = relevantSunspots
                .OrderBy(s => Math.Abs((s.TimeTag - flare.BeginTime!.Value).TotalMinutes))
                .First();

            return closestSunspot;
        }




        private static double CalculateSunspotFactor(SunspotData sunspotData)
        {
            if (sunspotData == null) return 1.0;


            double areaFactor = sunspotData.Area / 100.0; // Normalize area
            double spotFactor = sunspotData.NumSpot / 10.0; // Normalize number of spots


            double classFactor = sunspotData.SpotClass.StartsWith("A") ? 0.5 :
                                 sunspotData.SpotClass.StartsWith("B") ? 1.0 :
                                 sunspotData.SpotClass.StartsWith("C") ? 1.5 : 2.0;

            double magFactor = sunspotData.MagClass == "A" ? 0.5 :
                               sunspotData.MagClass == "B" ? 1.0 : 1.5;

            return (1 + areaFactor + spotFactor) * classFactor * magFactor;
        }



        /// <summary>
        /// calculates the surprise factor for the given flare event and CME speed
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cmeSpeed"></param>
        /// <returns></returns>
        private Task<double> CalculateSurpriseFactor(FlareEvent flare, double cmeSpeed ,SunspotData allSunspotData)
        {
            if (string.IsNullOrEmpty(flare.ClassType) || flare.ClassType.Length < 2)
                return Task.FromResult<double>(0);

            var flareClass = flare.ClassType[0];
            if (!expectedSpeedRanges.TryGetValue(flareClass, out (double Min, double Max) value))
                return Task.FromResult<double>(0);

            if (!double.TryParse(flare.ClassType.AsSpan(1), out var flareIntensity))
                return Task.FromResult<double>(0);

            var (minSpeed, maxSpeed) = value;

            // Adjust expected speed range based on specific flare intensity
            var adjustedMinSpeed = minSpeed * (1 + (flareIntensity - 1) * 0.1);
            var adjustedMaxSpeed = maxSpeed * (1 + (flareIntensity - 1) * 0.1);

          

            double surpriseFactor = 0;
            if (cmeSpeed < adjustedMinSpeed)
            {
                surpriseFactor = (adjustedMinSpeed - cmeSpeed) / adjustedMinSpeed;
            }
            else if (cmeSpeed > adjustedMaxSpeed)
            {
                surpriseFactor = (cmeSpeed - adjustedMaxSpeed) / adjustedMaxSpeed;
            }

          
            double sunspotFactor = allSunspotData != null ? CalculateSunspotFactor(allSunspotData) : 1.0;
            return Task.FromResult(surpriseFactor * sunspotFactor);
        }


        //TODO: Take into account linked cme events?
        // - What about active regions? or group by? 
        // - how to add sunspot data?
        /// <summary>
        /// Analyzes the given flare and CME events and returns a list of interesting events.
        /// </summary>
        /// <param name="flareEvents"></param>
        /// <param name="cmeEvents"></param>
        /// <param name="timeWindow"></param>
        /// <returns></returns>
        public async Task<List<InterestingEvent>> AnalyzeEventsAsync(List<FlareEvent> flareEvents, List<CMEEvent> cmeEvents)
        {
            var interestingEvents = new List<InterestingEvent>();
            var validFlares = flareEvents.Where(f => f.BeginTime != null && !string.IsNullOrEmpty(f.ClassType)).ToList();
            var allSunspotData = await GetAllSunspotData();



            foreach (var flare in validFlares)
            {
                var associatedCMEs = GetAssociatedCMEs(flare, cmeEvents);

                if (associatedCMEs.Count == 0)
                {
                    interestingEvents.Add(new InterestingEvent
                    {
                        Flare = flare,
                        CMESpeed = 0,
                        SurpriseFactor = 1.0,
                        Confidence = CalculateConfidenceLevel(flare, null),
                        Reason = "Flare without associated CME"
                    });
                    continue;
                }

                foreach (var cme in associatedCMEs)
                {
                    double cmeSpeed = CalculateCMESpeed(cme);
                    var relevantSunspotData = FindRelevantSunspotData(flare, allSunspotData);
                    var surpriseFactor = await CalculateSurpriseFactor(flare, cmeSpeed, relevantSunspotData);
                    double confidence = CalculateConfidenceLevel(flare, cme);

                    if (surpriseFactor > 0.5 || confidence < 0.7)
                    {
                    
                        string reason = DetermineReason(flare, cme, cmeSpeed, surpriseFactor, confidence, relevantSunspotData);
                        interestingEvents.Add(new InterestingEvent
                        {
                            Flare = flare,
                            CME = cme,
                            CMESpeed = cmeSpeed,
                            SurpriseFactor = surpriseFactor,
                            Confidence = confidence,
                            Reason = reason
                        });
                    }
                }

                CheckMultipleFlares(validFlares, flare, interestingEvents);
            }

            return [.. interestingEvents.OrderByDescending(e => e.SurpriseFactor)];
        }


        /// <summary>
        /// Determines the reason for the given flare event and CME speed
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cmeSpeed"></param>
        /// <param name="surpriseFactor"></param>
        /// <returns></returns>
        private string DetermineReason(FlareEvent flare, CMEEvent cme, double cmeSpeed, double surpriseFactor, double confidence, SunspotData allSunspotData)
        {
            string reason;
            if (string.IsNullOrEmpty(flare.ClassType) || flare.ClassType.Length == 0)
                reason = "Unknown flare class";
            else if (!expectedSpeedRanges.TryGetValue(flare.ClassType[0], out var speedRange))
                reason = "Unknown flare class";
            else
            {
                var (minSpeed, maxSpeed) = speedRange;
                if (cmeSpeed < minSpeed)
                    reason = $"Unusually slow CME for {flare.ClassType} flare class (Speed: {cmeSpeed} km/s, Expected min: {minSpeed} km/s)";
                else if (cmeSpeed > maxSpeed)
                    reason = $"Unusually fast CME for {flare.ClassType} flare class (Speed: {cmeSpeed} km/s, Expected max: {maxSpeed} km/s)";
                else
                    reason = $"Unexpected surprise factor for {flare.ClassType} flare class (Speed: {cmeSpeed} km/s, Expected range: {minSpeed}-{maxSpeed} km/s)";
            }

            reason += $" Surprise Factor: {surpriseFactor:F2}.";
 

            if (allSunspotData != null)
            {
                reason += $"Associated Sunspot Data:\n" +
              $"- Region: {allSunspotData.Region}\n" +
              $"- Area: {allSunspotData.Area} millionths of solar hemisphere\n" +
              $"- Number of Spots: {allSunspotData.NumSpot}\n" +
              $"- Spot Class: {allSunspotData.SpotClass ?? "Unknown"}\n" +
              $"- Magnetic Class: {allSunspotData.MagClass ?? "Unknown"}\n" +
              $"- Latitude: {allSunspotData.Latitude}\n" +
              $"- Longitude: {allSunspotData.Longitude}\n";

                if (allSunspotData.Area > 500)
                {
                    reason += "Observation: Large sunspot area, associated with higher flare probability.\n";
                }
                else if (allSunspotData.Area > 0)
                {
                    reason += "Observation: Small to moderate sunspot area.\n";
                }
                else
                {
                    reason += "Observation: No sunspot area data available.\n";
                }
            }
            else
            {
                reason += "No associated sunspot data found.\n";
            }

            return reason;
        }

        /// <summary>
        /// Checks if there are any CMEs associated with the given flare event
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cmeEvents"></param>
        /// <param name="timeWindow"></param>
        /// <returns></returns>
        private static List<float> FindAssociatedCMEs(FlareEvent flare, List<CMEEvent> cmeEvents, TimeSpan timeWindow)
        {
            return cmeEvents
                .Where(cme => cme.StartTime.HasValue && flare.BeginTime.HasValue && //TODO: add flare peak time?
                              cme.StartTime.Value >= flare.BeginTime.Value &&
                              (cme.StartTime.Value - flare.BeginTime.Value) <= timeWindow)
                .SelectMany(cme => cme.CMEAnalyses)
                .Select(analysis => analysis.Speed)
                .Where(speed => speed.HasValue)
                .Select(speed => speed.GetValueOrDefault(0f))
                .ToList();
        }


        /// <summary>
        ///  Check flareevents that are in quick succession
        ///  if 3 or more flares are in quick succession
        ///  add to interesting events list.
        /// </summary>
        /// <param name="allFlares"></param>
        /// <param name="currentFlare"></param>
        /// <param name="interestingEvents"></param>
        private static void CheckMultipleFlares(List<FlareEvent> allFlares, FlareEvent currentFlare, List<InterestingEvent> interestingEvents)
        {
            const int QuickSuccessionMinutes = 60; // Flares within 60 minutes are considered in quick succession
            const int MaxFlareCountForQuickSuccession = 3; // Number of flares to consider for quick succession

            if (currentFlare.BeginTime == null)
                return;

            var startTime = currentFlare.BeginTime.Value;
            var flareCount = 1;
            var nearbyFlares = allFlares
             .Where(f => f != currentFlare)
             .Where(f => f.BeginTime != null)
             .Where(f => Math.Abs((f.BeginTime?.Subtract(startTime))?.TotalMinutes ?? double.MaxValue) <= QuickSuccessionMinutes)
             .ToList();

            flareCount += nearbyFlares.Count;

            if (flareCount >= MaxFlareCountForQuickSuccession)
            {
                interestingEvents.Add(new InterestingEvent
                {
                    Flare = currentFlare,
                    CMESpeed = 0,
                    SurpriseFactor = 1.0,
                    Reason = $"{flareCount} flares in quick succession within {QuickSuccessionMinutes} minutes"
                });
            }
        }
    }
}

//TODO : move to its own data model file?
public class InterestingEvent
{
    public required FlareEvent Flare { get; set; }
    public double CMESpeed { get; set; } = double.NaN;
    public double SurpriseFactor { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; internal set; }
    public CMEEvent? CME { get; internal set; }
} 