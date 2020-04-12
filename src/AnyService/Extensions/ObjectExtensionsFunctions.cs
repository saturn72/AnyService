namespace System
{
    public static class ObjectExtensionsFunctions
    {
        public static T GetPropertyValueByName<T>(this object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            if (pi != null)
                return (T)pi.GetValue(obj);
            throw new InvalidOperationException();
        }

        public static T GetPropertyValueOrDefaultByName<T>(this object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            return pi != null ? (T)pi.GetValue(obj) : default(T);
        }
    }
}