﻿using Microsoft.AspNetCore.Mvc;
using SpaceWeatherApi.DataModels;
using SpaceWeatherApi.Utils;
using System.Text;
namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/sr")]
    public class SolarReportController(
        IApiClient apiClient,
        SolarReportingUtils solarReportingUtils,
        SpaceWeatherReportingUtils spaceWeatherReportingUtils)
       : BaseController(
           apiClient,
           solarReportingUtils: solarReportingUtils,
           spaceWeatherReportingUtils: spaceWeatherReportingUtils)
    {
        private async Task<(List<SolarRegionModel>, List<SunspotModel>)> FetchSolarDataAsync()
        {
            var solarRegions = await ApiClient.GetAllSolarRegionDataAsync();
            var sunspots = await ApiClient.GetAllSunspotDataAsync();

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

            var report = SolarReportingUtils?.GenerateSolarRegionReport(allSolarRegionData, allSunspotData);
            return report != null ? Ok(report) : StatusCode(500, "SolarReportingUtils is not initialized.");
        }



        [HttpGet("swa")]
        public async Task<IActionResult> GetSpaceWeatherReport()
        {
            var (allSolarRegionData, allSunspotData) = await FetchSolarDataAsync();
            var FlareData = await ApiClient.GetDataAsync("FLR") ?? [];
            var CMEData = await ApiClient.GetDataAsync("CME") ?? [];

            var allFlareEvents = FlareData.Cast<FlareEvent>().ToList();
            var allCMEEvents = CMEData.Cast<CMEEvent>().ToList();

            if (allSolarRegionData.Count == 0)
            {
                return NotFound("No solar region data available.");
            }

            var fluxData = await ApiClient.GetTodayFluxNum();

           return SpaceWeatherReportingUtils != null
        ? Content(SpaceWeatherReportingUtils.GenerateSpaceWeatherReport(
            allSolarRegionData,
            allSunspotData,
            allFlareEvents,
            allCMEEvents,
            fluxData), "text/plain")
        : StatusCode(500, "SpaceWeatherReportingUtils is not initialized.");



        }

    }
  }




