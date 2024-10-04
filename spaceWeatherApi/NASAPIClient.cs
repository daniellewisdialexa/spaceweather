using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SkiaSharp;
using spaceWeatherApi.DataModels;
using System.Globalization;
using System.Net.Http;

namespace spaceWeatherApi
{
    public class NasaApiClient(HttpClient httpClient, IOptions<AppSettings> appSettings)
    {
 
        private readonly AppSettings _appSettings = appSettings.Value;



        /// <summary>
        /// Mapping of endpoint names to their respective event types
        /// </summary>
        public static readonly Dictionary<string, Type> EndpointTypeMap = new()
        {
            { "FLR", typeof(FlareEvent) },
            { "CME", typeof(CMEEvent) },
        };


        protected async Task<List<T>> FetchDONKIDataAsync<T>(string endpoint, DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryParameters = new List<string>();

            if (startDate.HasValue)
            {
                queryParameters.Add($"startDate={startDate:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                queryParameters.Add($"endDate={endDate:yyyy-MM-dd}");
            }

            queryParameters.Add($"api_key={_appSettings.IdentitySettings.ApiKey}");

            var queryString = string.Join("&", queryParameters);
            var fullUrl = new Uri(new Uri(_appSettings.ConnectionStrings.DONKIBaseURL), $"{endpoint}?{queryString}");

            try
            {
                var response = await httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON data into a list of T objects
                var events = JsonConvert.DeserializeObject<List<T>>(data);

                return events ?? [];
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}  Full URL was: {fullUrl}");
                return [];
            }
        }

        //TODO move all Get data methods to API client class (rename class) 
        /// <summary>
        /// Base method to retrieve data from the NASA API
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="startDate"></param> 
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<List<object>?> GetDataAsync(string endpoint, string? startDate = null, string? endDate = null)
        {
            if (!EndpointTypeMap.TryGetValue(endpoint, out var eventType))
            {
                return null;
            }

            var (parsedStartDate, parsedEndDate) = ParseDateTime(startDate, endDate);

            try
            {
                return eventType switch
                {
                    Type t when t == typeof(FlareEvent) =>
                        (await FetchDONKIDataAsync<FlareEvent>(endpoint, parsedStartDate, parsedEndDate))
                            .Cast<object>().ToList(),

                    Type t when t == typeof(CMEEvent) =>
                        (await FetchDONKIDataAsync<CMEEvent>(endpoint, parsedStartDate, parsedEndDate))
                            .Cast<object>().ToList(),

                    _ => null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching DONKI data: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// Parsing the start and end date strings into DateTime objects.   
        /// startDate param accepts strings: "today", "yr{number}", "yyyy-MM-dd"
        /// </summary>
        /// <param name="startDate"></param> 
        /// <param name="endDate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected (DateTime parsedStartDate, DateTime parsedEndDate) ParseDateTime(string? startDate, string? endDate)
        {
            DateTime parsedStartDate;
            DateTime parsedEndDate;

            if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
            {
                parsedStartDate = DateTime.UtcNow.AddDays(-30);
                parsedEndDate = DateTime.UtcNow;
            }
            else if (startDate != null && startDate.Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                parsedStartDate = DateTime.UtcNow;
                parsedEndDate = DateTime.UtcNow;
            }
            else if (startDate != null && startDate.StartsWith("yr", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(startDate.AsSpan(2), out int years))
                {
                    parsedStartDate = DateTime.UtcNow.AddYears(-years);
                    parsedEndDate = DateTime.UtcNow;
                }
                else
                {
                    throw new ArgumentException("Invalid year format in start date", nameof(startDate));
                }
            }
            else
            {
                parsedStartDate = DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sDate)
                    ? sDate
                    : throw new ArgumentException("Invalid start date format", nameof(startDate));

                parsedEndDate = string.IsNullOrEmpty(endDate)
                    ? DateTime.UtcNow
                    : DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime eDate)
                        ? eDate
                        : throw new ArgumentException("Invalid end date format", nameof(endDate));
            }

            return (parsedStartDate, parsedEndDate);
        }

        public async Task<List<T>> GetNOAAData<T>(string endpoint)
        {
            return endpoint switch
            {
                "sunspot" => await FetchNOAAData<T>("json/sunspot_report.json"),
                _ => new List<T>()
            };
        }

        protected async Task<List<T>> FetchNOAAData<T>(string endpoint)
        {
            try
            {
                string baseUrl = _appSettings.ConnectionStrings.NOAABaseURl;
                string url = new Uri(new Uri(baseUrl), endpoint).ToString();

                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                };

                return JsonConvert.DeserializeObject<List<T>>(jsonResponse, settings) ?? new List<T>();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching NOAA data: {ex.Message}");
                return new List<T>();
            }
        }
    }
}