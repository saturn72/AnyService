namespace System.Text.Json
{
    public static class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement jsonElement)
        {
            return StringExtensions.ToObject<T>(jsonElement.ToString());
        }

        public static object ToObject(this JsonElement jsonElement, Type toType)
        {
            return StringExtensions.ToObject(jsonElement.ToString(), toType);
        }
    }
}
