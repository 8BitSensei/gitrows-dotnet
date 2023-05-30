using System.Dynamic;

namespace gitrows_dotnet.utils
{
    public static class Util
    {
        public static bool TryParsePropertyAsInt(this ExpandoObject expandoObject, string key, out int result)
        {
            var propertyValue = (expandoObject as IDictionary<string, object>)[key].ToString();
            return int.TryParse(propertyValue, out result);
        }
    }
}
