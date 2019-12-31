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
    }
}