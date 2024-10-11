using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace SpaceWeatherApi.Controllers
{
    public abstract class BaseController(ApiClient ApiClient) : ControllerBase
    {  //TODO: Go over and check all access modifiers for all classes/methods
        public readonly ApiClient _ApiClient = ApiClient;

        /// <summary>
        /// Order enum for ordering data
        /// </summary>
        public enum Order
        {
            Asc,
            Desc
        }

        /// <summary>
        /// Filter data based on the filter string array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static List<T> FilterData<T>(List<T> data, string[] filter)
        {
            if (filter == null || filter.Length == 0) return data;
            {
                foreach (var filterItem in filter)
                {
                    var filterParts = filterItem.Split('='); 
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
        public static List<object> OrderBy(List<object> data, Order order, string property)
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
        public static int CountOfProperty<T>(List<T> data, string propertyName)
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
