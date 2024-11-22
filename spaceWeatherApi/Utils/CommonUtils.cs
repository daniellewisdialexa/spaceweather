using System.Reflection;

namespace SpaceWeatherApi.Utils
{
    public class CommonUtils
    {
        

        /// <summary>
        /// Order enum for ordering data
        /// </summary>
        public enum Order
        {
            Asc,
            Desc
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
    }
}
