using System.Dynamic;
using System.Runtime.InteropServices;
using System.Linq;

namespace gitrows_dotnet.utils
{
    public static class Extensions
    {
        public static bool TryParsePropertyAsDouble(this ExpandoObject expandoObject, string key, out double result)
        {
            var propertyValue = (expandoObject as IDictionary<string, object>)[key].ToString();
            return double.TryParse(propertyValue, out result);
        }

        public static bool TryParsePropertyAsInt(this ExpandoObject expandoObject, string key, out int result)
        {
            var propertyValue = (expandoObject as IDictionary<string, object>)[key].ToString();
            return int.TryParse(propertyValue, out result);
        }

        public static bool TryParsePropertyAsString(this ExpandoObject expandoObject, string key, out string result)
        {
            var propertyValue = (expandoObject as IDictionary<string, object>)[key].ToString();
            if (!string.IsNullOrEmpty(propertyValue))
            {
                result = propertyValue;
                return true;
            }

            result = string.Empty;
            return false;
        }

        public static bool TryGetAverageAsInt(this List<ExpandoObject> expandoList, string key, out int average) 
        {
            int total = 0;
            foreach(var @object in expandoList) 
            {
                if (!@object.TryParsePropertyAsInt(key, out var propertyAsInt)) {
                    average = 0;
                    return false;
                }

                total += propertyAsInt;
            }

            average = total / expandoList.Count;
            return true;
        }

        public static bool TryGetAverageAsDouble(this List<ExpandoObject> expandoList, string key, out double average)
        {
            double total = 0;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsDouble(key, out var propertyAsInt))
                {
                    average = 0;
                    return false;
                }

                total += propertyAsInt;
            }

            average = total / expandoList.Count;
            return true;
        }

        public static bool TryGetSumAsInt(this List<ExpandoObject> expandoList, string key, out int sum)
        {
            int total = 0;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsInt(key, out var propertyAsInt))
                {
                    sum = 0;
                    return false;
                }

                total += propertyAsInt;
            }

            sum = total;
            return true;
        }

        public static bool TryGetSumAsDouble(this List<ExpandoObject> expandoList, string key, out double sum)
        {
            double total = 0;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsDouble(key, out var propertyAsInt))
                {
                    sum = 0;
                    return false;
                }

                total += propertyAsInt;
            }

            sum = total;
            return true;
        }

        public static bool TryGetMinAsInt(this List<ExpandoObject> expandoList, string key, out int min)
        {
            int? tmp = null;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsInt(key, out var propertyAsInt))
                {
                    min = 0;
                    return false;
                }

                if(tmp == null)
                    tmp = propertyAsInt;
                else if (propertyAsInt < tmp)
                    tmp = propertyAsInt;
            }

            min = tmp == null ? 0 : (int)tmp;
            return true;
        }

        public static bool TryGetMinAsDouble(this List<ExpandoObject> expandoList, string key, out double min)
        {
            double? tmp = null;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsDouble(key, out var propertyAsInt))
                {
                    min = 0;
                    return false;
                }

                if (tmp == null)
                    tmp = propertyAsInt;
                else if (propertyAsInt < tmp)
                    tmp = propertyAsInt;
            }

            min = tmp == null ? 0 : (double)tmp;
            return true;
        }

        public static bool TryGetMaxAsInt(this List<ExpandoObject> expandoList, string key, out double max)
        {
            int? tmp = null;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsInt(key, out var propertyAsInt))
                {
                    max = 0;
                    return false;
                }

                if (tmp == null)
                    tmp = propertyAsInt;
                else if (propertyAsInt > tmp)
                    tmp = propertyAsInt;
            }

            max = tmp == null ? 0 : (int)tmp;
            return true;
        }

        public static bool TryGetMaxAsDouble(this List<ExpandoObject> expandoList, string key, out double max)
        {
            double? tmp = null;
            foreach (var @object in expandoList)
            {
                if (!@object.TryParsePropertyAsDouble(key, out var propertyAsInt))
                {
                    max = 0;
                    return false;
                }

                if (tmp == null)
                    tmp = propertyAsInt;
                else if (propertyAsInt > tmp)
                    tmp = propertyAsInt;
            }

            max = tmp == null ? 0 : (double)tmp;
            return true;
        }

        public static bool TrySelectProperties(this List<ExpandoObject> expandoList, string key, out List<ExpandoObject> result)
        {
            result = new List<ExpandoObject>();
            var span = CollectionsMarshal.AsSpan(expandoList);
            for (var i = 0; i < span.Length; i++)
            {
                result.AddRange(from KeyValuePair<string, object> kvp in span[i]
                             where kvp.Key.Equals(key)
                             let newColumn = new ExpandoObject()
                             where newColumn.TryAdd(kvp.Key, kvp.Value)
                             select newColumn);
            }

            return true;
        }
    }
}
