using SpaceWeatherApi.DataModels;
using SpaceWeatherApi.Services.Interfaces;
using System.Text;

namespace SpaceWeatherApi.Services
{
    public class SpaceWeatherService : ISpaceWeatherService
        {
            public string GenerateSpaceWeatherReport(
                List<SolarRegionModel> allSolarRegionData,
                List<SunspotModel> allSunspotData,
                List<FlareEvent> allFlareEvents,
                List<CMEEvent> allCMEEvents,
                FluxModel fluxData)
            {
                var report = GenerateRegionReportModel(allSolarRegionData, allSunspotData, allFlareEvents, allCMEEvents);
                return FormatTextReport(report, fluxData);
            }


            /// <summary>
            /// Generate the region report model
            /// </summary>
            /// <param name="allSolarRegionData"></param>
            /// <param name="allSunspotData"></param>
            /// <param name="allFlareEvents"></param>
            /// <param name="allCMEEvents"></param>
            /// <returns>Returns region report</returns>
            private static List<RegionReportModel> GenerateRegionReportModel(
            List<SolarRegionModel> allSolarRegionData,
            List<SunspotModel> allSunspotData,
            List<FlareEvent> allFlareEvents,
            List<CMEEvent> allCMEEvents)
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

                var report = allSolarRegionData
                    .Where(sr => sr.ObservedDate >= thirtyDaysAgo && sr.Region.HasValue)
                    .GroupBy(sr => sr.Region)
                    .Select(g => new RegionReportModel
                    {
                        Region = g.Key,
                        TotalSunspots = CalculateSunspots(allSunspotData, g.Key, thirtyDaysAgo),
                        RecentSunspots = CalculateSunspots(allSunspotData, g.Key, sevenDaysAgo),
                        SignificantFlares = GetSignificantFlares(allFlareEvents, g.Key, thirtyDaysAgo),
                        RecentSignificantFlares = GetSignificantFlares(allFlareEvents, g.Key, sevenDaysAgo),
                        CMECount = CountCMEs(allCMEEvents, g.Key, thirtyDaysAgo),
                        RecentCMECount = CountCMEs(allCMEEvents, g.Key, sevenDaysAgo)
                    })
                   .OrderByDescending(r => r.TotalSunspots +
                            (r.SignificantFlares?.Count ?? 0) * 10 +
                            r.CMECount * 5).ToList();

            AddNullRegionCMEs(report, allCMEEvents, thirtyDaysAgo, sevenDaysAgo);

                return report;
            }


            /// <summary>
            /// Calculate the sunspots for a region and date range
            /// </summary>
            /// <param name="allSunspotData"></param>
            /// <param name="region"></param>
            /// <param name="startDate"></param>
            /// <returns>Returns a list of all the sunspots, added together, for each region </returns>
            private static double CalculateSunspots(List<SunspotModel> allSunspotData, int? region, DateTime startDate)
            {
                return allSunspotData
                    .Where(ss => ss.Region == region && DateTime.TryParse(ss.Obsdate, out var date) && date >= startDate)
                    .Sum(ss => ss.NumSpot);
            }

            /// <summary>
            /// Get the most significant flares for a region and date range
            /// </summary>
            /// <param name="allFlareEvents"></param>
            /// <param name="region"></param>
            /// <param name="startDate"></param>
            /// <returns>Returns a list of the flares that are m or x class for each region</returns>
            private static List<FlareEvent> GetSignificantFlares(List<FlareEvent> allFlareEvents, int? region, DateTime startDate)
            {
                return allFlareEvents
                    .Where(f =>
                       f.ActiveRegionNum?.ToString()?.EndsWith(region?.ToString() ?? "") ?? false &&
                        f.BeginTime >= startDate &&
                        f.ClassType != null &&
                        (f.ClassType.StartsWith("M", StringComparison.OrdinalIgnoreCase) ||
                         f.ClassType.StartsWith("X", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            /// <summary>
            /// Count the CMEs for a region and date range
            /// </summary>
            /// <param name="allCMEEvents"></param>
            /// <param name="region"></param>
            /// <param name="startDate"></param>
            /// <returns>Returns a list of counted cmes for each region</returns>
            private static int CountCMEs(List<CMEEvent> allCMEEvents, int? region, DateTime startDate)
            {
                return allCMEEvents.Count(c =>
                {
                    string? cmeRegion = c.ActiveRegionNum?.ToString();
                    string? solarRegion = region?.ToString();

                    if (cmeRegion == null && solarRegion == null) return true;
                    if (cmeRegion == null || solarRegion == null) return false;

                    return cmeRegion.EndsWith(solarRegion) && c.StartTime >= startDate;
                });
            }

            private static void AddNullRegionCMEs(List<RegionReportModel> report, List<CMEEvent> allCMEEvents, DateTime thirtyDaysAgo, DateTime sevenDaysAgo)
            {
                var nullRegionCMEs = allCMEEvents.Count(c => c.ActiveRegionNum == null && c.StartTime >= thirtyDaysAgo);
                if (nullRegionCMEs > 0)
                {
                    report.Add(new RegionReportModel
                    {
                        Region = -1,
                        TotalSunspots = 0,
                        RecentSunspots = 0,
                        SignificantFlares = new List<FlareEvent>(),
                        RecentSignificantFlares = new List<FlareEvent>(),
                        CMECount = nullRegionCMEs,
                        RecentCMECount = allCMEEvents.Count(c => c.ActiveRegionNum == null && c.StartTime >= sevenDaysAgo)
                    });
                }
            }
            /// <summary>
            /// Handles text report formating
            /// </summary>
            /// <param name="report"></param>
            /// <param name="fluxData"></param>
            /// <returns>Returns a text report</returns>
            private static string FormatTextReport(List<RegionReportModel> report, FluxModel fluxData)
            {
                var textReport = new StringBuilder();

                // Add solar flux data
                if (fluxData != null && !string.IsNullOrEmpty(fluxData.Flux))
                {
                    textReport.AppendLine($"Solar Flux: {fluxData.Flux} sfu");
                }
                else
                {
                    textReport.AppendLine("Solar Flux: N/A");
                }

                textReport.AppendLine();
                textReport.AppendLine("** 30 days worth of data, ordered by last 7 days activity **");
                textReport.AppendLine("| Region | Total SSN | M/X Flares | Strongest Flare | CME | Note |");
                textReport.AppendLine("|--------|-----------|------------|-----------------|-----|------|");

                foreach (var region in report)
                {
                    string? regionDisplay = region.Region == -1 ? "NORE" : region.Region.ToString();
                    var strongestFlare = region.SignificantFlares?.OrderByDescending(f => f.ClassType).FirstOrDefault();
                    var activityTrend = DetermineActivityTrend(region.TotalSunspots, region.SignificantFlares?.Count ?? 0, region.CMECount);
                    textReport.AppendLine($"| {regionDisplay,6} | {region.TotalSunspots,9:F0} | {region.SignificantFlares?.Count,10} | {strongestFlare?.ClassType ?? "N/A",15} | {region.CMECount,3} | {activityTrend,-20} |");
                }

                return textReport.ToString();
            }

            /// <summary>
            /// Helps determine the activity trend of the sunspots, flares, and cmes
            /// </summary>
            /// <param name="sunspots"></param>
            /// <param name="flares"></param>
            /// <param name="cmes"></param>
            /// <returns>returns a score for each data point</returns>
            private static string DetermineActivityTrend(double sunspots, int flares, int cmes)
            {
                var activityScore = sunspots + flares * 10 + cmes * 5;

                if (activityScore > 1000) return "Very Active";
                if (activityScore > 500) return "Active";
                if (activityScore > 100) return "Moderately Active";
                if (activityScore > 0) return "Slightly Active";
                return "Inactive";
            }
        }
    }