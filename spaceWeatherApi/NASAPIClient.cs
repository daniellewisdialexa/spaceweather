using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using spaceweather;

namespace spaceWeatherApi
{
    public class NasaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;

        public NasaApiClient(HttpClient httpClient, IOptions<AppSettings> appSettings)
        {
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
        }

        public async Task<List<T>>GetDataAsync<T>(string endpoint, DateTime? startDate = null, DateTime? endDate = null)
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
                var response = await _httpClient.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON data into a list of T objects
                var events = JsonConvert.DeserializeObject<List<T>>(data);

                return events ?? new List<T>();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}  Full URL was: {fullUrl}");
                return new List<T>();
            }
        }
    }
}