using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SpaceWeatherApi.DataModels;
using SpaceWeatherApi.Utils;
using System.Globalization;

namespace SpaceWeatherApi
{
    public interface IApiClient
    {
        Task<List<T>> FetchDONKIDataAsync<T>(string endpoint, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<object>?> GetDataAsync(string endpoint, string? startDate = null, string? endDate = null);
        Task<List<SunspotModel>> GetAllSunspotDataAsync();
        Task<FluxModel> GetTodayFluxNum();
        Task<List<SolarRegionModel>> GetAllSolarRegionDataAsync();
        Task<List<T>> GetNOAADataAsync<T>(string endpoint) where T : class;
        Task<T?> FetchSingleNOAADataAsync<T>(string path) where T : class;
        Task<List<T>> FetchNOAADataAsync<T>(string endpoint);
    }

    public class ApiClient(HttpClient httpClient, IAppSettings appSettings,DateParseUtils dateUtils) : IApiClient
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IAppSettings _appSettings = appSettings;

        /// <summary>
        /// Mapping of endpoint names to their respective event types
        /// </summary>
        public static readonly Dictionary<string, Type> EndpointTypeMap = new()
        {
            { "FLR", typeof(FlareEvent) },
            { "CME", typeof(CMEEvent) },
        };


        /// <summary>
        /// Fetch data from the DONKI API
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<List<T>> FetchDONKIDataAsync<T>(string endpoint, DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryParameters = new List<string>();
            if (startDate.HasValue || endDate.HasValue)
            {
                queryParameters.Add(
                    $"{(startDate.HasValue ? $"startDate={startDate:yyyy-MM-dd}" : string.Empty)}" +
                    $"{(endDate.HasValue ? $"&endDate={endDate:yyyy-MM-dd}" : string.Empty)}"
                );
            }

            queryParameters.Add($"api_key={_appSettings.IdentitySettings.ApiKey}");
            var queryString = string.Join("&", queryParameters);
            var fullUrl = new Uri(new Uri(_appSettings.ConnectionStrings.DONKIBaseURL), $"{endpoint}?{queryString}");
            var response = await _httpClient.GetAsync(fullUrl);
           
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<T>>(data);
            return events ?? [];
        }

        /// <summary>
        /// Base method to retrieve data from the NASA API
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="startDate"></param> 
        /// <param name="endDate"></param>
        /// <returns>List of typed events </returns>
        public async Task<List<object>?> GetDataAsync(string endpoint, string? startDate = null, string? endDate = null)
        {
            if (!EndpointTypeMap.TryGetValue(endpoint, out var eventType))
            {
                return null;
            }

            var (parsedStartDate, parsedEndDate) = dateUtils.ParseDateTime(startDate, endDate);

            try
            {
                return await FetchDataForType(eventType, endpoint, parsedStartDate, parsedEndDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching DONKI data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetch data for the given event type
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="endpoint"></param>
        /// <param name="parsedStartDate"></param>
        /// <param name="parsedEndDate"></param>
        /// <returns></returns>
        private async Task<List<object>?> FetchDataForType(Type eventType, string endpoint, DateTime? parsedStartDate, DateTime? parsedEndDate)
        {
            if (eventType == typeof(FlareEvent))
            {
                return (await FetchDONKIDataAsync<FlareEvent>(endpoint, parsedStartDate, parsedEndDate))
                    .Cast<object>().ToList();
            }
            else if (eventType == typeof(CMEEvent))
            {
                return (await FetchDONKIDataAsync<CMEEvent>(endpoint, parsedStartDate, parsedEndDate))
                    .Cast<object>().ToList();
            }
            return null;
        }

        //---- NOAA Data ----\\
        /// <summary>
        /// Get all the sunspot data 
        /// </summary>
        /// <returns></returns>
        public async Task<List<SunspotModel>> GetAllSunspotDataAsync()
        {
            return await GetNOAADataAsync<SunspotModel>("sunspot");
        }

        /// <summary>
        /// Get the today's flux number
        /// </summary>
        /// <returns></returns>
        public async Task<FluxModel> GetTodayFluxNum()
        {
            return (await GetNOAADataAsync<FluxModel>("fluxtoday")).Single();
        }

        /// <summary>
        /// Get all the solar region data
        /// </summary>
        /// <returns></returns>
        public async Task<List<SolarRegionModel>> GetAllSolarRegionDataAsync()
        {
            return await GetNOAADataAsync<SolarRegionModel>("solarregion");
        }

        /// <summary>
        /// Maps the endpoint to the respective NOAA API path
        /// </summary>
        private readonly Dictionary<string, string> _endpointMap = new()
            {
                ["sunspot"] = "json/sunspot_report.json",
                ["solarregion"] = "json/solar_regions.json",
                ["fluxtoday"] = "products/summary/10cm-flux.json"
       };

        /// <summary>
        /// Get data from the NOAA depending on endpoint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns>NOAA data</returns>
        public async Task<List<T>> GetNOAADataAsync<T>(string endpoint) where T : class
        {
            if (!_endpointMap.TryGetValue(endpoint, out var path))
            {
                return [];
            }

            if (endpoint == "fluxtoday")
            {
                var result = await FetchSingleNOAADataAsync<T>(path);
                return result != null ? [result] : [];
            }

            return await FetchNOAADataAsync<T>(path);
        }


        /// <summary>
        ///  Fetch single object of NOAA data - Used mainly for fetching today's flux number
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns> 
        /// Deserialized  JSON object of type T
        /// </returns>
        public async Task<T?> FetchSingleNOAADataAsync<T>(string path) where T : class
        {
            string baseUrl = _appSettings.ConnectionStrings.NOAABaseURl;
            string url = new Uri(new Uri(baseUrl), path).ToString();

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(jsonResponse);
         
        }


        /// <summary>
        /// Fetch data from the NOAA API
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns>NOAA json data</returns>
        public async Task<List<T>> FetchNOAADataAsync<T>(string endpoint)
        {
            try
            {
                string baseUrl = _appSettings.ConnectionStrings.NOAABaseURl;
                string url = new Uri(new Uri(baseUrl), endpoint).ToString();

                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                };

                return JsonConvert.DeserializeObject<List<T>>(jsonResponse, settings) ?? [];
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching NOAA data: {ex.Message}");
                return [];
            }
        }

     
    }
}
