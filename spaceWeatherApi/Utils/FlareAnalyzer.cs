using SpaceWeatherApi.DataModels;

namespace SpaceWeatherApi.Utils
{
    public class FlareAnalyzer
    {
        private readonly IApiClient _apiClient;
        private readonly IAppSettings _appSettings;
        private readonly Dictionary<char, (double Min, double Max)> _expectedSpeedRangesCollection;

        public FlareAnalyzer(IApiClient apiClient, IAppSettings appSettings)
        {
            _apiClient = apiClient;
            _appSettings = appSettings;
            _expectedSpeedRangesCollection = _appSettings.DataValues.ExpectedSpeedRanges
                .ToDictionary(kvp => kvp.Key[0], kvp => (kvp.Value.Min, kvp.Value.Max));
        }


        /// <summary>
        ///  Get all CME events that are associated with the given flare event.
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="allCMEs"></param>
        /// <returns>List of cme events</returns>
        private List<CMEEvent> GetAssociatedCMEs(FlareEvent flare, List<CMEEvent> allCMEs)
        {
            var timeWindow = TimeSpan.FromHours(_appSettings.DataValues.CME_ASSOCIATION_WINDOW_HOURS);

            return allCMEs
                .Where(cme =>
                    cme.StartTime.HasValue &&
                    flare.BeginTime.HasValue &&
                    flare.PeakTime.HasValue &&
                    cme.StartTime.Value >= flare.BeginTime.Value &&
                    cme.StartTime.Value <= flare.PeakTime.Value.Add(timeWindow))
                .ToList();
        }


        /// <summary>
        /// Calculates the confidence level for the given flare and CME event.
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cme"></param>
        /// <returns>Calculated confidence number</returns>
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


        /// <summary>
        /// Calculates the speed of the given CME event. If no speed is available, returns 0.0.
        /// </summary>
        /// <param name="cme"></param>
        /// <returns>Calculated speed of cme number</returns>
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


        /// <summary>
        /// Find the relevant sunspot data for the given flare event.
        /// If no sunspot data is available, returns null.
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="allSunspotData"></param>
        /// <returns>Relevant sunpot</returns>
        private static SunspotModel? FindRelevantSunspotData(FlareEvent flare, List<SunspotModel> allSunspotData)
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

            if (relevantSunspots.Count == 0 || flare.BeginTime == null)
            {
                return null;
            }

            var closestSunspot = relevantSunspots
                .Where(s => s.TimeTag.HasValue)
                .OrderBy(s => Math.Abs((s.TimeTag!.Value - flare.BeginTime.Value).TotalMinutes))
                .FirstOrDefault();

            return closestSunspot;
        }




        /// <summary>
        /// Calculates the sunspot factor for the given sunspot data.
        /// </summary>
        /// <param name="sunspotData"></param>
        /// <returns> Calcuated spot factor number </returns>
        private static double CalculateSunspotFactor(SunspotModel sunspotData)
        {
            if (sunspotData == null) return 1.0;

            double areaFactor = sunspotData.Area / 100.0; // Normalize area
            double spotFactor = sunspotData.NumSpot / 10.0; // Normalize number of spots

            double classFactor = sunspotData.SpotClass switch
            {
                string s when s.Length > 0 && s[0] == 'A' => 0.5,
                string s when s.Length > 0 && s[0] == 'B' => 1.0,
                string s when s.Length > 0 && s[0] == 'C' => 1.5,
                _ => 2.0
            };

                double magFactor = sunspotData.MagClass switch
            {
                "A" => 0.5,
                "B" => 1.0,
                _ => 1.5
            };

            return (1 + areaFactor + spotFactor) * classFactor * magFactor;
        }




        /// <summary>
        /// calculates the surprise factor for the given flare event and CME speed
        /// 0-5: Low surprise (event is as expected)
        /// 5-10: Moderate surprise
        /// 10-20: High surprise
        /// 20+: Extremely surprising or unusual event
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cmeSpeed"></param>
        /// <returns> Calcluated Suprise factor number </returns>
        private Task<double> CalculateSurpriseFactor(FlareEvent flare, double cmeSpeed ,SunspotModel allSunspotData)
        {
            if (string.IsNullOrEmpty(flare.ClassType) || flare.ClassType.Length < 2)
                return Task.FromResult<double>(0);

            var flareClass = flare.ClassType[0];
            if (!_expectedSpeedRangesCollection.TryGetValue(flareClass, out (double Min, double Max) value))
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

  
        /// <summary>
        /// Analyzes the given flare and CME events and returns a list of interesting events.
        /// </summary>
        /// <param name="flareEvents"></param>
        /// <param name="cmeEvents"></param>
        /// <param name="timeWindow"></param>
        /// <returns>A list of events that might be interesting</returns>
        public async Task<List<InterestingEvent>> AnalyzeEventsAsync(List<FlareEvent> flareEvents, List<CMEEvent> cmeEvents)
        {
            var interestingEvents = new List<InterestingEvent>();
            var validFlares = flareEvents.Where(f => f.BeginTime != null && !string.IsNullOrEmpty(f.ClassType)).ToList();
            var allSunspotData = await _apiClient.GetAllSunspotDataAsync();

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
                    var relevantSunspotData = FindRelevantSunspotData(flare, allSunspotData) ?? new SunspotModel();
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
        /// Format the given coordinate, latitude or longitude.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="isLatitude"></param>
        /// <returns>Formated coordinates</returns>
        private static string FormatLatLong(double coordinate, bool isLatitude)
        {
            char direction = isLatitude ? (coordinate >= 0 ? 'N' : 'S') : (coordinate >= 0 ? 'E' : 'W');
            coordinate = Math.Abs(coordinate);
            int degrees = (int)coordinate;
            double minutesDecimal = (coordinate - degrees) * 60;
            int minutes = (int)minutesDecimal;
            int seconds = (int)((minutesDecimal - minutes) * 60);

            return $"{degrees}° {minutes}' {seconds}\" {direction}";
        }

        /// <summary>
        /// Format the given angle
        /// </summary>
        /// <param name="angle"></param>
        /// <returns>Fromated angle coordinate</returns>
        private static string FormatAngle(double angle)
        {
            angle = Math.Abs(angle); // Ensure angle is positive
            int degrees = (int)angle;
            double minutesDecimal = (angle - degrees) * 60;
            int minutes = (int)minutesDecimal;
            int seconds = (int)((minutesDecimal - minutes) * 60);

            return $"{degrees}° {minutes}' {seconds}\"";
        }

        /// <summary>
        /// Get the description of the given magnetic class.
        /// </summary>
        /// <param name="magClass"></param>
        /// <returns>String description of magnetic class</returns>
        private string GetMagneticClassDescription(string magClass)
        {
            if (string.IsNullOrEmpty(magClass))
                return "Unknown";

            if (_appSettings.DataValues.MagneticClassDescriptions.TryGetValue(magClass, out var description))
            {
                return description;
            }

            return "Unrecognized magnetic classification";
        }


        /// <summary>
        /// Derive the reason for the given flare and CME event.
        /// </summary>
        /// <param name="flare"></param>
        /// <param name="cme"></param>
        /// <param name="cmeSpeed"></param>
        /// <param name="surpriseFactor"></param>
        /// <param name="confidence"></param>
        /// <param name="allSunspotData"></param>
        /// <returns>String for determined reason of surprised factor</returns>
        private string DetermineReason(FlareEvent flare, CMEEvent cme, double cmeSpeed, double surpriseFactor, double confidence, SunspotModel allSunspotData)
        {

            string surpriseDescription = surpriseFactor switch
            {
                < 5 => "Low",
                < 10 => "Moderate",
                < 20 => "High",
                _ => "Extremely High"
            };

            string reason;

            if (string.IsNullOrEmpty(flare.ClassType) || flare.ClassType.Length == 0)
                reason = "Unknown flare class";
            else if (!_expectedSpeedRangesCollection.TryGetValue(flare.ClassType[0], out var speedRange))
                reason = "Unknown flare class";
            else
            {
                var (minSpeed, maxSpeed) = speedRange;
                if (cmeSpeed < minSpeed)
                    reason = $"Unusually slow CME for {flare.ClassType} flare class (Speed: {cmeSpeed:F1} km/s, Expected min: {minSpeed} km/s)";
                else if (cmeSpeed > maxSpeed)
                    reason = $"Unusually fast CME for {flare.ClassType} flare class (Speed: {cmeSpeed:F1} km/s, Expected max: {maxSpeed} km/s)";
                else
                    reason = $"Unexpected surprise factor for {flare.ClassType} flare class (Speed: {cmeSpeed:F1} km/s, Expected range: {minSpeed}-{maxSpeed} km/s)";
            }

            reason += $"\nSurprise Factor: {surpriseFactor:F2} ({surpriseDescription})";
            reason += $"\nConfidence Level: {confidence * 100:F2}%";
         
            // Add CME details
            reason += $"\nCME Details:";
            reason += $"\n- Start Time: {cme.StartTime?.ToString() ?? "Unknown"}";

            if (cme.CMEAnalyses != null && cme.CMEAnalyses.Count > 0)
            {
                reason += $"\n- Number of CME Analyses: {cme.CMEAnalyses.Count}";

                for (int i = 0; i < cme.CMEAnalyses.Count; i++)
                {
                    var analysis = cme.CMEAnalyses[i];
                    reason += $"\n\nCME Analysis {i + 1}:";
                    reason += $"\n- Type: {analysis.Type ?? "Unknown"}";
                    reason += $"\n- Is Most Accurate: {analysis.IsMostAccurate}";

                    if (analysis.HalfAngle.HasValue)
                        reason += $"\n- Half Angle: {analysis.HalfAngle.Value:F1}° ({FormatAngle(Convert.ToDouble(analysis.HalfAngle.Value))})";

                    if (analysis.Speed.HasValue)
                        reason += $"\n- Speed: {analysis.Speed.Value:F1} km/s";

                    if (analysis.Latitude.HasValue)
                        reason += $"\n- Latitude: {FormatLatLong(Convert.ToDouble(analysis.Latitude.Value), true)}";

                    if (analysis.Longitude.HasValue)
                        reason += $"\n- Longitude: {FormatLatLong(Convert.ToDouble(analysis.Longitude.Value), true)}";

                    if (analysis.Tilt.HasValue)
                        reason += $"\n- Tilt: {analysis.Tilt.Value:F1} degrees";

                    if (analysis.MinorHalfWidth.HasValue)
                        reason += $"\n- Minor Half Width: {analysis.MinorHalfWidth.Value:F1}";

                    if (analysis.SpeedMeasuredAtHeight.HasValue)
                        reason += $"\n- Speed Measured At Height: {analysis.SpeedMeasuredAtHeight.Value:F1}";

                    reason += $"\n- Feature Code: {analysis.FeatureCode}";
                    reason += $"\n- Measurement Technique: {analysis.MeasurementTechnique}";
               
                }

                // Highlight the most accurate analysis if available
                var mostAccurateAnalysis = cme.CMEAnalyses.FirstOrDefault(a => a.IsMostAccurate);
                if (mostAccurateAnalysis != null)
                {
                    reason += $"\n\nMost Accurate CME Analysis:";
                    reason += $"\n- Type: {mostAccurateAnalysis.Type ?? "Unknown"}";
                    reason += $"\n- Time: {mostAccurateAnalysis.Time21_5.ToString() ?? "Unkown"}";
                    reason += $"\n- Half Angle: {FormatAngle(Convert.ToDouble(mostAccurateAnalysis.HalfAngle))}";
                    reason += $"\n- Latitude: {FormatLatLong(Convert.ToDouble(mostAccurateAnalysis.Latitude ?? 0), true)}";
                    reason += $"\n- Longitude: {FormatLatLong(Convert.ToDouble(mostAccurateAnalysis.Longitude ?? 0), false)}";
                    reason += $"\n- SpeedMeasuredAtHeight: {mostAccurateAnalysis.SpeedMeasuredAtHeight ?? 0} km/s";
                    reason += $"\n- Speed: {mostAccurateAnalysis.Speed?.ToString("F1") ?? "Unknown"} km/s";
                    reason += $"\n- Note: {mostAccurateAnalysis.Note ?? "Unknown"}";
                    reason += $"\n- Link: {mostAccurateAnalysis.Link ?? "Unknown"}";
                }
            }
            else
            {
                reason += "\n- No detailed CME analysis available";
            }

            // Notes for the speed used in calculations
            reason += $"\n\nSpeed used in calculations: {cmeSpeed:F1} km/s";


            // Add note about confidence level
            if (confidence < 0.5)
                reason += "\nNote: Low confidence in this event association.";
            else if (confidence > 0.8)
                reason += "\nNote: High confidence in this event association.";

            if (allSunspotData != null)
            {
     
                reason += $"\nAssociated Sunspot Data:";
                reason += $"\n- TimeTag: {allSunspotData.TimeTag}";
                reason += $"\n- Region: {allSunspotData.Region}";
                reason += $"\n- Area: {allSunspotData.Area} millionths of solar hemisphere";
                reason += $"\n- Number of Spots: {allSunspotData.NumSpot}";
                reason += $"\n- Spot DataUtils: {allSunspotData.SpotClass ?? "Unknown"}";
                reason += $"\n- Magnetic DataUtils: {GetMagneticClassDescription(allSunspotData.MagClass)?? "Unknown"}; ";
                reason += $"\n- Latitude: {FormatLatLong(Convert.ToDouble(allSunspotData.Latitude), true)}";
                reason += $"\n- Longitude: {FormatLatLong(Convert.ToDouble(allSunspotData.Longitude), false)}";

                if (allSunspotData.Area > 500)
                    reason += "\nObservation: Large sunspot area, associated with higher flare probability.";
                else if (allSunspotData.Area > 0)
                    reason += "\nObservation: Small to moderate sunspot area.";
                else
                    reason += "\nObservation: No sunspot area data available.";
            }
            else
            {
                reason += "\nNo associated sunspot data found.";
            }

            return reason;
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
