using Microsoft.AspNetCore.Mvc;
using spaceWeatherApi;
using spaceWeatherApi.DataModels;
using System.Globalization;
using System.Reflection;

namespace SpaceWeatherApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController(NasaApiClient nasaApiClient) : ControllerBase
    {
        protected readonly NasaApiClient _nasaApiClient = nasaApiClient;

        /// <summary>
        /// Mapping of endpoint names to their respective event types
        /// </summary>
        public static readonly Dictionary<string, Type> EndpointTypeMap = new()
        {
            { "FLR", typeof(FlareEvent) },
            { "CME", typeof(CMEEvent) }

        };

        /// <summary>
        /// Order enum for ordering data
        /// </summary>
        public enum Order
        {
            Asc,
            Desc
        }


        /// <summary>
        /// Base method to retrieve data from the NASA API
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="startDate"></param> 
        /// <param name="endDate"></param>
        /// <returns></returns>
        protected async Task<List<object>?> GetDataAsync(string endpoint, string? startDate = null, string? endDate = null)
        {
            var (parsedStartDate, parsedEndDate) = ParseDateTime(startDate, endDate);

            if (!EndpointTypeMap.TryGetValue(endpoint, out var eventType))
            {
                return null;
            }

            var method = typeof(NasaApiClient).GetMethod("GetDataAsync")?.MakeGenericMethod(eventType);

            if (method?.Invoke(_nasaApiClient, [endpoint, parsedStartDate, parsedEndDate]) is not Task task)
            {
                return null;
            }

            // Await the task and get the result
            await task.ConfigureAwait(false);

            // Use reflection to get the result property of the task
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty != null)
            {
                var data = resultProperty.GetValue(task);
                if (data is IEnumerable<object> enumerable)
                {
                    return enumerable.ToList();
                }
            }

            return null;
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


        /// <summary>
        /// Filter data based on the filter string array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        protected static List<T> FilterData<T>(List<T> data, string[] filter)
        {
            if (filter == null || filter.Length == 0) return data;
            else
            {
                foreach (var filterItem in filter)
                {
                    var filterParts = filterItem.Split('='); //TODO: add support for linked event filtering
                    if (filterParts.Length == 2)
                    {
                        var filterProperty = filterParts[0];
                        var filterValue = filterParts[1];
                        data = data
                            .Where(fe =>
                            {
                                if (fe == null) return false;

                                var propertyInfo = fe.GetType().GetProperty(filterProperty);
                                if (propertyInfo == null) return false;

                                var propertyValue = propertyInfo.GetValue(fe, null);
                                if (propertyValue == null) return false;
                                return propertyValue.ToString()?.Contains(filterValue, StringComparison.OrdinalIgnoreCase) ?? false;

                            }).ToList();
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Order data based on the property and order enum (Asc/Desc)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="order"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected static List<object> OrderBy(List<object> data, Order order, string property)
        {
            if (data == null || data.Count == 0)
            {
                return [];
            }

            var orderedData = data
                .Where(item =>
                {
                    var itemType = item.GetType();
                    var propertyInfo = itemType.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    return propertyInfo != null && propertyInfo.GetValue(item, null) != null;
                })
                .OrderBy(item =>
                {

                    var itemType = item.GetType();
                    var propertyInfo = itemType.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                                       ?? throw new ArgumentException($"Property '{property}' does not exist on type '{itemType.Name}'.");

                    return propertyInfo.GetValue(item, null);
                });

            if (order == Order.Desc)
            {
                return orderedData.Reverse().ToList();
            }

            return [.. orderedData];
        }

        /// <summary>
        /// Group data based on the property
        /// </summary>
        /// <param name="data"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Dictionary<object, List<object>> GroupByProperty(List<object> data, string property)
        {
            var groupedData = data
                .GroupBy(item =>
                {
                    var itemType = item.GetType();
                    var propertyInfo = itemType.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                                       ?? throw new ArgumentException($"Property '{property}' does not exist on type '{itemType.Name}'.");
                    return propertyInfo.GetValue(item, null) ?? new object();
                })
                .ToDictionary(g => g.Key, g => g.ToList());
            return groupedData;
        }


        /// <summary>
        /// Get a count of any specific property from the return data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected static int CountOfProperty<T>(List<T> data, string propertyName)
        {
            if (data == null || data.Count == 0)
            {
                return 0;
            }

            return data
                .Where(item => item != null)
                .Count(item =>
                {
                    if (item == null)
                    {
                        return false;
                    }

                    var propertyInfo = item.GetType().GetProperty(propertyName);
                    return propertyInfo != null && propertyInfo.GetValue(item) != null;
                });
        }

    }
}
