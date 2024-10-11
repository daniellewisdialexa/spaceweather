using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.DataModels;
using Microsoft.Extensions.Options;
using System.Text;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController(ApiClient apiClient, IOptions<AppSettings> appSettings) : BaseController(apiClient)
    {
        private readonly AppSettings _appSettings = appSettings.Value;
        private readonly ApiClient _apiClient = apiClient;

        private async Task<(List<SolarRegionModel> SolarRegions, List<SunspotModel> Sunspots)> FetchSolarDataAsync()
        {
            var solarRegions = await _apiClient.GetAllSolarRegionDataAsync();
            var sunspots = await _apiClient.GetAllSunspotDataAsync();

            // Add some logging or debugging here
            Console.WriteLine($"Fetched {solarRegions.Count} solar regions and {sunspots.Count} sunspots");

            return (solarRegions, sunspots);
        }

        [HttpGet("solarregionreport")]
        public async Task<IActionResult> GetSolarRegionReport()
        {
            var (allSolarRegionData, allSunspotData) = await FetchSolarDataAsync();

            if (allSolarRegionData.Count == 0)
            {
                return NotFound("No solar region data available.");
            }
            //TODO: move this to own method? 
            var report = allSolarRegionData
                .Select(sr => new
                {
                    sr.ObservedDate,
                    sr.Region,
                    NumberSpots = allSunspotData
                        .Where(ss => ss.Region == sr.Region)
                        .Sum(ss => ss.NumSpot),
                    sr.SpotClass,
                    sr.FirstDate,
                    MatchingSunspots = allSunspotData
                        .Where(ss => ss.Region == sr.Region)
                        .Select(ss => new
                        {
                            ss.Obsdate,
                            ss.NumSpot,
                            ss.SpotClass
                        })
                        .ToList()
                })
                .OrderByDescending(r => r.ObservedDate)
                .ToList();

            return Ok(report);
        }


        [HttpGet("swa")]
        public async Task<IActionResult> GetSpaceWeatherReport()
        {
            //Get all the data for the last 30 days
            var (allSolarRegionData, allSunspotData) = await FetchSolarDataAsync();
            var FlareData =  await _ApiClient.GetDataAsync("FLR" ) ?? [];
            var CMEData =  await _ApiClient.GetDataAsync("CME") ?? [];

            var allflareEvents = FlareData.Cast<FlareEvent>().ToList();
            var allCMEEvents = CMEData.Cast<CMEEvent>().ToList();

            //TODO: move all logic to their own methods
            //TODO: better data checks here
            if (allSolarRegionData.Count == 0)
            {
                return NotFound("No solar region data available.");
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var test = allflareEvents.Where(f => f.ClassType.StartsWith("M")).ToList();

            var report = allSolarRegionData
                .Where(sr => sr.ObservedDate >= thirtyDaysAgo && sr.Region.HasValue)
                .GroupBy(sr => sr.Region)
                .Select(g => new
                {
                    Region = g.Key,
                    TotalSunspots = allSunspotData
                        .Where(ss => ss.Region == g.Key && DateTime.TryParse(ss.Obsdate, out var date) && date >= thirtyDaysAgo)
                        .Sum(ss => ss.NumSpot ),
                    RecentSunspots = allSunspotData
                        .Where(ss => ss.Region == g.Key && DateTime.TryParse(ss.Obsdate, out var date) && date >= sevenDaysAgo)
                        .Sum(ss => ss.NumSpot ),
                    SignificantFlares = allflareEvents
                          .Where(f =>
                          {
                              if (f.ActiveRegionNum == null) return false;

                              string? flareRegion = f.ActiveRegionNum.ToString();
                              string? solarRegion = g.Key.ToString();

                              return flareRegion.EndsWith(solarRegion) &&
                                     f.BeginTime >= thirtyDaysAgo &&
                                     (f.ClassType.StartsWith("M", StringComparison.OrdinalIgnoreCase) ||
                                      f.ClassType.StartsWith("X", StringComparison.OrdinalIgnoreCase));
                          }).ToList(),

                    RecentSignificantFlares = allflareEvents
                        .Where(f => f.ActiveRegionNum == g.Key && f.BeginTime >= sevenDaysAgo && (f.ClassType.StartsWith("M") || f.ClassType.StartsWith("X")))
                        .ToList(),
                    CMECount = allCMEEvents.Count(c =>
                    {
                        string? cmeRegion = c.ActiveRegionNum?.ToString();
                        string? solarRegion = g.Key?.ToString();

                        if (cmeRegion == null && solarRegion == null) return true; 
                        if (cmeRegion == null || solarRegion == null) return false; 

                        return cmeRegion.EndsWith(solarRegion) && c.StartTime >= thirtyDaysAgo;
                    }),
                    RecentCMECount = allCMEEvents.Count(c => c.ActiveRegionNum == g.Key && c.StartTime >= sevenDaysAgo)
                })
                   .OrderByDescending(r => r.TotalSunspots + r.SignificantFlares.Count * 10 + r.CMECount * 5).ToList();



            //CME data can contain a lot of nulls, this gives its own entry in the report 
            var nullRegionCMEs = allCMEEvents.Count(c => c.ActiveRegionNum == null && c.StartTime >= thirtyDaysAgo);
            if (nullRegionCMEs > 0)
            {
                report.Add(new
                {
                    Region = (int?) -1,  
                    TotalSunspots = 0d,
                    RecentSunspots = 0d,
                    SignificantFlares = new List<FlareEvent>(),
                    RecentSignificantFlares = new List<FlareEvent>(),
                    CMECount = nullRegionCMEs,
                    RecentCMECount = allCMEEvents.Count(c => c.ActiveRegionNum == null && c.StartTime >= sevenDaysAgo)
                });
            }


            // Create a text report
            var textReport = new StringBuilder();
            // Add solar flux data
            var fluxData = await _apiClient.GetTodayFluxNum();
            if (fluxData != null && !string.IsNullOrEmpty(fluxData.Flux))
            {
                //sfu = 10-22W m-2 Hz-1)
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
                var strongestFlare = region.SignificantFlares.OrderByDescending(f => f.ClassType).FirstOrDefault();
                var activityTrend = DetermineActivityTrend(region.TotalSunspots, region.SignificantFlares.Count, region.CMECount);

                textReport.AppendLine($"| {regionDisplay,6} | {region.TotalSunspots,9:F0} | {region.SignificantFlares.Count,10} | {strongestFlare?.ClassType ?? "N/A",15} | {region.CMECount,3} | {activityTrend,-20} |");
            }
            return Ok(textReport.ToString());
        }


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




